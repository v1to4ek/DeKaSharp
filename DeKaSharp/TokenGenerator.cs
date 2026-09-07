using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp
{
    internal class TokenGenerator
    {
        private CancellationTokenSource? _tokenSource;

        public TokenGenerator() => _tokenSource = null;

        public CancellationToken GetOrCreateAndGet()
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

        public void Cancel()
        {
            if (_tokenSource == null) throw new InvalidOperationException("Не создан источник токена отмены");

            _tokenSource.Cancel();

            Logger.Log("Токен отменён");
        }

        public void Clear()
        {
            if (_tokenSource == null) throw new InvalidOperationException("Не создан источник токена отмены");

            _tokenSource.Dispose();

            _tokenSource = null;

            Logger.Log("Токен очищен");
        }
    }
}
