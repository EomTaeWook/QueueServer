using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using GameUtils;
using System.Text.Json;

namespace QueueHubServer.Service
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class SecurityService
    {
        private int _lastKeyUpdateDayOfYear;
        private string _currentPublicKey;
        public SecurityService()
        {
            RegenerateSecurityKeys();
        }
        private void RegenerateSecurityKeys()
        {
            var keyPair = Cryptogram.GenerateKeyPair();
            _lastKeyUpdateDayOfYear = DateTime.Now.DayOfYear;
            _currentPublicKey = keyPair.Item1;
            Cryptogram.InitializeWithPrivateKey(keyPair.Item2);
            Cryptogram.InitializeWithPublicKey(keyPair.Item1);
        }
        public string GetCurrentPublicKey()
        {
            if (_lastKeyUpdateDayOfYear < DateTime.Now.DayOfYear)
            {
                RegenerateSecurityKeys();
            }
            return _currentPublicKey;
        }
        public string DecryptString(string text)
        {
            if (_lastKeyUpdateDayOfYear < DateTime.Now.DayOfYear)
            {
                RegenerateSecurityKeys();
            }
            try
            {
                return Cryptogram.DecryptString(text);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return string.Empty;
            }
        }
        public string Encrypt<T>(T token)
        {
            if (_lastKeyUpdateDayOfYear < DateTime.Now.DayOfYear)
            {
                RegenerateSecurityKeys();
            }
            var json = JsonSerializer.Serialize(token);
            try
            {
                return Cryptogram.EncryptString(json);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return string.Empty;
            }
        }
    }
}
