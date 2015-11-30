using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SensorMonitor.Host
{

    public class ServiceControl
    {
        private static bool IsInstalled(string serviceName)
        {
            using (ServiceController controller =
                new ServiceController(serviceName))
            {
                try
                {
                    ServiceControllerStatus status = controller.Status;
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        private static bool IsRunning(string serviceName)
        {
            using (ServiceController controller =
                new ServiceController(serviceName))
            {
                if (!IsInstalled(serviceName))
                    return false;
                return (controller.Status == ServiceControllerStatus.Running);
            }
        }

        private static AssemblyInstaller GetInstaller(System.Reflection.Assembly assem)
        {
            AssemblyInstaller installer = new AssemblyInstaller(
                assem, null);
            installer.UseNewContext = true;
            return installer;
        }

        public static void InstallService(string serviceName, System.Reflection.Assembly assem)
        {
            if (IsInstalled(serviceName)) return;

            try
            {
                using (AssemblyInstaller installer = GetInstaller(assem))
                {
                    System.Collections.IDictionary state =
                        new System.Collections.Hashtable();
                    try
                    {
                        installer.Install(state);
                        installer.Commit(state);
                    }
                    catch
                    {
                        try
                        {
                            installer.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static void UninstallService(string serviceName, System.Reflection.Assembly assem)
        {
            if (!IsInstalled(serviceName)) return;
            try
            {
                using (AssemblyInstaller installer = GetInstaller(assem))
                {
                    System.Collections.IDictionary state =
                        new System.Collections.Hashtable();
                    try
                    {
                        installer.Uninstall(state);
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static void StartService(string serviceName)
        {
            if (!IsInstalled(serviceName)) return;

            using (ServiceController controller =
                new ServiceController(serviceName))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Running)
                    {
                        controller.Start();
                        controller.WaitForStatus(ServiceControllerStatus.Running,
                            TimeSpan.FromSeconds(10));
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        public static void StopService(string serviceName)
        {
            if (!IsInstalled(serviceName)) return;
            using (ServiceController controller =
                new ServiceController(serviceName))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Stopped)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped,
                             TimeSpan.FromSeconds(10));
                    }
                }
                catch
                {
                    throw;
                }
            }
        }
    }
}
