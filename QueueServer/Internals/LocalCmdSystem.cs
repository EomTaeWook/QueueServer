using Dignus.DependencyInjection.Attributes;
using QueueServer.Internals.Interface;

namespace QueueServer.Internals
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class LocalCmdSystem : CommandSystem.LocalCmdModule, IModule
    {
        private CommandSystem.LocalCmdModule _localCmd;
        public LocalCmdSystem(CommandSystem.LocalCmdModule localCmd)
        {
            _localCmd = localCmd;
        }

        public Task StartAsync()
        {
            return Task.Run(() =>
            {
                _localCmd.Run();
            });
        }
    }
}
