using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp
{
    internal class CancellationTokenGenerator : IAsyncCleanable
    {
        private CancellationTokenSource? _tokenSource;

        private readonly Lock _locker;

        public CancellationTokenGenerator()
        {
            _tokenSource = null; 

            _locker = new ();
        }

        public CancellationToken GetOrCreateAndGet()
        {
            lock(_locker)
            {
                if (_tokenSource == null)
                {
                    _tokenSource = new CancellationTokenSource();

                    Logger.Log("Создан источник токена отмены");
                }

                var token = _tokenSource.Token;

                Logger.Log("Получен токен отмены");

                return token;
            }
        }

        public void Cancel()
        {
            lock(_locker)
            {
                if (_tokenSource == null) throw new InvalidOperationException("Не создан источник токена отмены");

                _tokenSource.Cancel();

                Logger.Log("Токен отменён");
            }
        }

        private void Clear()
        {
            Logger.Log("Очистка источника токена отмены");

            lock (_locker)
            {
                if (_tokenSource == null) throw new InvalidOperationException("Не создан источник токена отмены");

                _tokenSource.Dispose();

                _tokenSource = null;

                Logger.Log("Токен очищен");
            }

            Logger.Log("Очистка источника токена отмены завершена");
        }

        public Task CleanAsync() => Task.Run(Clear);
    }
}
