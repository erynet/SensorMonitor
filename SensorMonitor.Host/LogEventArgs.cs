using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace SensorMonitor.Host
{
    class LogEventArgs : EventArgs
    {
        // EventArgs 를 통해서 넘어가는 로그 메시지 객체를 정의한다.

        // 메시지의 종류를 정의한다.
        public enum MessageType { Comment = 0, Info = 1, Debug = 2, Warning = 3, Error = 4 };


        // GlobalVariables.AllowLogLevel 의 수치에 따라서, 
        // 해당 객체가 생성은 되었으되 비어 있을 수도 있다.
        // 성능을 위해서 사용한다
        private bool isValid;
        // 전달되는 메시지 자체
        private string message;
        // 로그가 작성된 시점의 시간
        private DateTime dateTime;
        // 메시지의 종류
        private MessageType messageType;
        // 호출 당시의 StackTrace
        // 디버그를 위해 넣긴 하지만, 성능 저하의 우려가 상당함.
        private StackTrace stackTrace;

        // Getter 들

        public string Message
        {
            get
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    StreamWriter s = new StreamWriter(stream);
                    s.Write(dateTime.ToString("[yyyy-MM-dd HH:mm:ss] "));
                    s.Write(message);

                    return s.ToString();
                }
                //return dateTime.ToString("[yyyy-MM-dd HH:mm:ss] ") + message;
            }
        }

        public string MessageWithIndent
        {
            get
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    StreamWriter s = new StreamWriter(stream);
                    s.Write(dateTime.ToString("[yyyy-MM-dd HH:mm:ss] "));
                    for (int i = 0; i < Depth; i++)
                        s.Write("\t");
                    s.Write(message);

                    return s.ToString();
                }
            }
        }

        public string MessageOnly
        {
            get
            {
                return message;
            }
        }

        public DateTime When
        {
            get
            {
                return dateTime;
            }
        }

        public int Depth
        {
            get
            {
                return stackTrace.FrameCount;
            }
        }

        public MessageType Category
        {
            get
            {
                return messageType;
            }
        }

        public StackTrace GetStackTrace
        {
            get
            {
                return stackTrace;
            }
        }

        public List<string> StackTraces
        {
            get
            {
                List<string> s = new List<string>();
                StackFrame sf;
                for (int i = stackTrace.FrameCount; i >= 0; i--)
                {
                    sf = stackTrace.GetFrame(i);
                    StringBuilder sb = new StringBuilder();

                    var method = sf.GetMethod();
                    sb.Append(method.DeclaringType.ToString());
                    sb.Append(".");
                    sb.Append(method.Name);

                    var parameters = method.GetParameters();
                    sb.Append("(");
                    for (int j = 0; j < parameters.Length; ++j)
                    {
                        if (j > 0)
                            sb.Append(", ");
                        var parameter = parameters[j];
                        sb.Append(parameter.ParameterType.Name);
                        sb.Append(" ");
                        sb.Append(parameter.Name);
                    }
                    sb.Append(")");

                    var sourceFileName = sf.GetFileName();
                    if (!string.IsNullOrEmpty(sourceFileName))
                    {
                        sb.Append(" in ");
                        sb.Append(sourceFileName);
                        sb.Append(": line ");
                        sb.Append(sf.GetFileLineNumber().ToString());
                    }

                    s.Add(sb.ToString());
                }

                return s;
            }
        }


        public LogEventArgs(string message, MessageType messageType, StackTrace st)
        {
            this.message = message;
            this.dateTime = DateTime.Now;
            this.messageType = messageType;
            this.stackTrace = st;
        }

        public LogEventArgs(string message, StackTrace st) : this(message, MessageType.Comment, st) { }
    }
}
