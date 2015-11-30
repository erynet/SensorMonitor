using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using System.ServiceProcess;
using System.Security.Principal;
using System.Configuration.Install;
using System.Diagnostics;
using SensorMonitor.Core.Event;

namespace SensorMonitor.Host
{
    public class Hybrid
    {
        static void Main(string[] args)
        {
            bool useAppointedLibrary = false;

            var options = new CommandLineOptions();
            
            if (args.Length == 0)
            {
                // 인자가 하나도 없을 때는 그냥 기본값으로 작동한다.
            }
            else
            {
                // 여기선 인자를 일단 커맨드라인 파서로 넣고

                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    // 문제 없음.

                    if (options.InstallService || options.UninstallService)
                    {
                        // 서비스 인스톨, 혹은 언인스톨

                        if (options.InstallService)
                        {
                            // 인스톨을 하고, 서비스를 켜준다.
                            if (!CheckPrivilege())
                            {
                                Console.WriteLine("This action require administrator privilege");
                                Environment.Exit(0);
                            }
                            ServiceControl.InstallService("SensorMonitor(ASTP)", typeof(ClientService).Assembly);
                            ServiceControl.StartService("SensorMonitor(ASTP)");
                        }
                        else
                        {
                            // 언인스톨을 해준다.
                            if (!CheckPrivilege())
                            {
                                Console.WriteLine("This action require administrator privilege");
                                Environment.Exit(0);
                            }
                            ServiceControl.StopService("SensorMonitor(ASTP)");
                            ServiceControl.UninstallService("SensorMonitor(ASTP)", typeof(ClientService).Assembly);
                        }
                        // 인스톨/언인스톨 후에 종료
                        Environment.Exit(0);
                    }

                    useAppointedLibrary = options.LibName != "NoSuchValue";
                }
                else
                {
                    // 사용법을 잘못 넣었다면 그냥 종료
                    // 사용법이 틀린 시점에 이미 Usage 가 출력된다.
                    Environment.Exit(0);
                }
            }


            if (Environment.UserInteractive)
            {
                var core = new Core();

                if (useAppointedLibrary)
                {
                    if (!core.Attach(options.LibName))
                    {
                        Console.WriteLine("Error occur in initializing sub components");
                        Environment.Exit(0);
                    }
                }
                else
                {
                    if (!core.Attach())
                    {
                        Console.WriteLine("Error occur in initializing sub components");
                        Environment.Exit(0);
                    }
                }
                
                core.Log += LogEventTerminalReceiver;
                
                Console.CancelKeyPress += new ConsoleCancelEventHandler(
                    delegate (object sender, ConsoleCancelEventArgs arg)
                    {
                        Console.WriteLine("SIGINT Received");

                        core.Dispose();
                        arg.Cancel = true;
                    }
                );

                var t = new Thread(core.Run);
                t.Start();
            }
            else
            {
                // 서비스 모드로 동작한다.
                // 즉 콘솔이 아니다.
                // 얘는 인자를 먹지 않는다.
                // 즉 라이브러리 지정도 안된다.
                var cs = new ClientService();
                ServiceBase.Run(cs);
            }

        }

        static bool CheckPrivilege()
        {
            var identity = WindowsIdentity.GetCurrent();
            if (identity == null) return false;
            var principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void LogEventTerminalReceiver(object sender, EventArgs evt)
        {
            string msg;
            var e = (LogEvt)evt;

            switch (e.Category)
            {
                case LogEvt.MessageType.Comment:
                    // 그냥 내가 하고 싶은 말 띄우기
                    msg = $"[{e.When:MM/dd/yy H:mm:ss zzz}] [{"Comment"}] [{e.From}] {e.Message}";
                    break;
                case LogEvt.MessageType.Debug:
                    // 디버그 목적으로 남기는 메시지
                    msg = $"[{e.When:MM/dd/yy H:mm:ss zzz}] [{"Debug  "}] [{e.From}] {e.Message}";
                    break;
                case LogEvt.MessageType.Info:
                    // 정보 출력 목적으로 남기는 메시지
                    msg = $"[{e.When:MM/dd/yy H:mm:ss zzz}] [{"Info   "}] [{e.From}] {e.Message}";
                    break;
                case LogEvt.MessageType.Warning:
                    // Assert 구문에 해당하는 부분을 통과하지 못했을때 남기는 메시지
                    msg = $"[{e.When:MM/dd/yy H:mm:ss zzz}] [{"Warning"}] [{e.From}] {e.Message}";
                    break;
                case LogEvt.MessageType.Error:
                    // Exception 에 catch 되었을때 남기는 메시지
                    msg = $"[{e.When:MM/dd/yy H:mm:ss zzz}] [{"Error  "}] [{e.From}] {e.Message}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.WriteLine(msg);
        }
    }

    public class ClientService : ServiceBase
    {
        private Core _core;

        //private EventLog eventLog;

        private System.ComponentModel.IContainer components = null;

        public ClientService()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            ServiceName = "SensorMonitor(ASTP)";
            
            _core = new Core();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnStart(string[] args)
        {
            // 처리할일 처리하고(Client.Core 를 초기화)

            if (!_core.Attach()) return;
            _core.Log += LogEventTerminalReceiver;

            var t = new Thread(_core.Run);
            t.Start();

            base.OnStart(args);
        }

        protected override void OnStop()
        {
            // 처리할일 처리하고(Cient.Core 를 Dispose())
            _core.Dispose();
            base.OnStop();
        }

        void LogEventTerminalReceiver(object sender, EventArgs evt)
        {
            string msg;
            var e = (LogEvt)evt;

            switch (e.Category)
            {
                case LogEvt.MessageType.Comment:
                    // 그냥 내가 하고 싶은 말 띄우기
                    msg = $"[{"Comment"}] [{e.From}] {e.Message}";
                    EventLog.WriteEntry(msg);
                    break;
                case LogEvt.MessageType.Debug:
                    // 디버그 목적으로 남기는 메시지
                    msg = $"[{"  Debug"}] [{e.From}] {e.Message}";
                    EventLog.WriteEntry(msg);
                    break;
                case LogEvt.MessageType.Info:
                    // 정보 출력 목적으로 남기는 메시지
                    msg = $"[{"   Info"}] [{e.From}] {e.Message}";
                    EventLog.WriteEntry(msg);
                    break;
                case LogEvt.MessageType.Warning:
                    // Assert 구문에 해당하는 부분을 통과하지 못했을때 남기는 메시지
                    msg = $"[{"Warning"}] [{e.From}] {e.Message}";
                    EventLog.WriteEntry(msg);
                    break;
                case LogEvt.MessageType.Error:
                    // Exception 에 catch 되었을때 남기는 메시지
                    msg = $"[{"Comment"}] [{e.From}] {e.Message}";
                    EventLog.WriteEntry(msg);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.WriteLine(msg);
        }
    }

    [RunInstallerAttribute(true)]
    public class ClientServiceInstaller : Installer
    {
        public ClientServiceInstaller()
        {
            ServiceProcessInstaller processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };
            ServiceInstaller serviceInstaller = new ServiceInstaller
            {
                StartType = ServiceStartMode.Automatic,
                ServiceName = "SensorMonitor(ASTP)",
                Description = "ASTP Sensor Monitoring service"
            };
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }
}
