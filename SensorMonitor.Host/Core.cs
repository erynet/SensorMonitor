using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;

using SensorMonitor.Core;
using SensorMonitor.Core.Event;
using SensorMonitor.Core.Interfaces;
//using SensorMonitor.Util;

namespace SensorMonitor.Host
{
    public class Core : IDisposable
    {
        public readonly string NameSpace = "SensorMonitor.Host";

        // 위에서 언급된 하위 어셈블리를 탐색하기 위해, 현재 자기 자신의 실행 경로를 추출하여 저장한다.
        private readonly string _currentEntryAssemblyPath;

        // 파일명을 기반으로 해당 어셈블리에 들어있는 네임스페이스를 추측하고, 이를 저장한다.
        private string _assemblyFullpath;
        private string _assemblyFilename;
        private string _assemblyNameSpace;
    
        private readonly BusHub _busHub;
        public event EventHandler Log;

        private List<ISubComponent> _subComponents; 

        private bool _loopContinue;

        public Core()
        {
            // 먼저 현재 실행중인 어셈블리의 폴더(exe)의 경로를 찾는다.
            _currentEntryAssemblyPath = 
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            _busHub = new BusHub();

            _subComponents = new List<ISubComponent>();
            _loopContinue = true;
        }

        public bool Attach(string libName = null)
        {
            string assemblyFullpath;
            string assemblyFilename;
            string assemblyNameSpace;
            
            Regex rx;
            Assembly asm;
            Type asmType;
            DynamicInvoker inv;
            ISubComponent component;

            string[] fileNames = Directory.GetFiles(_currentEntryAssemblyPath);

            if (libName == null)
            {
                rx = new Regex(@"(SensorMonitor.Component.\w+.\w+).dll", 
                    RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            else
            {
                rx = new Regex($@"(SensorMonitor.Component.{libName}.\w+).dll", 
                    RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            var assemblyCandidates = (from file in fileNames
                                         where rx.IsMatch(file)
                                         orderby file
                                         select new
                                         {
                                             fullpath = file,
                                             filename = rx.Match(file).Groups[0].Value,
                                             ns = rx.Match(file).Groups[1].Value
                                         }).ToList();
            if (!assemblyCandidates.Any())
                return false;

            foreach (var candidate in assemblyCandidates)
            {
                assemblyFullpath = candidate.fullpath;
                assemblyFilename = candidate.filename;
                assemblyNameSpace = candidate.ns;
                
                try
                {
                    asm = Assembly.LoadFrom(assemblyFullpath);
                    asmType = asm.GetType(assemblyNameSpace + ".Entry");
                    inv = new DynamicInvoker(asmType);
                    component = inv.CreateInstance() as ISubComponent;
                }
                catch (Exception e)
                {
                    string msg = "Assembly.LoadFrom(dataAssemblyFullpath) / Exceptiuon : " + e.ToString();
                    L(msg, LogEvt.MessageType.Error);
                    continue;
                }

                if (component != null)
                {
                    component.ConnectBus(_busHub);
                    component.Log += SubComponentLogEvtConsumer;
                    _subComponents.Add(component);
                }
            }
            return true;
        }

        public void Run()
        {
            foreach (var subComponent in _subComponents)
            {
                if (!subComponent.Initialize())
                    return;
            }

            while (_loopContinue)
            {
                Thread.Sleep(500);
            }

            foreach (var subComponent in _subComponents)
            {
                subComponent.Dispose();
            }
        }

        
        public void Dispose()
        {
            L("SIGINT Received", LogEvt.MessageType.Info);
            _loopContinue = false;
        }

        private void SubComponentLogEvtConsumer(object sender, EventArgs e)
        {
            Log?.Invoke(this, e);
        }

        public void L(string message, LogEvt.MessageType type = LogEvt.MessageType.Comment)
        {
            Log?.Invoke(this, new LogEvt(message, type, new StackTrace(), NameSpace));
        }
    }
}
