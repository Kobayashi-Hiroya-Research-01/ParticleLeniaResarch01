using System;
using System.IO;

#if UNITY_EDITOR

using UnityEngine;

#endif

namespace Assets.ParticleLeniaResarch01.Analysis
{
    public partial class DebugTextWriter
    {
        public enum EndCode
        {
            NOT_OPEN = 0,
            OPEN = 1,
            WRITERING = 2,
            CLOSE = 3,
            CLOSED = 4,
        }

        private StreamWriter writer;
        private readonly bool killed = false;

        public DebugTextWriter(string textName, bool killed = false)
        {
            TextName = textName;
            var path = $"../../_D_{TextName}.txt";
            FileInfo = new FileInfo(path);
            Line = 0;
            this.killed = killed;
        }

        public string TextName { get; }

        public FileInfo FileInfo { get; }

        public int Line { get; private set; }

        public EndCode Append(Func<string> content, int startLine, int endLine)
        {
            if (killed) return EndCode.NOT_OPEN;
            Line++;

            if (Line < startLine)
            {
                return EndCode.NOT_OPEN;
            }
            else if (Line > endLine)
            {
                return EndCode.CLOSED;
            }

            if (startLine <= Line && Line <= endLine)
            {
                var contentText = $"{Line,5} : {content()}\n";
                if (writer == null)
                {
                    File.WriteAllText(FileInfo.FullName, contentText);
                    writer = FileInfo.AppendText();
                }
                else
                {
                    writer.Write(contentText);
                }
            }

            if (Line == startLine)
            {
                return EndCode.OPEN;
            }
            else if (Line == endLine)
            {
                writer.Close();
                return EndCode.CLOSE;
            }
            else
            {
                return EndCode.WRITERING;
            }
        }

        public EndCode AppendWithLog(Func<string> content, int startLine, int endLine)
        {
            if (killed) return EndCode.NOT_OPEN;
#if UNITY_EDITOR
            return AppendWithUnityDebug(content, startLine, endLine);
#else
            return AppendWithConsole(content, startLine, endLine);
#endif
        }

        public EndCode AppendWithConsole(Func<string> content, int startLine, int endLine)
        {
            var result = Append(content, startLine, endLine);

            switch (result)
            {
                case EndCode.OPEN:
                    Console.WriteLine($"Open Debug Text  : {FileInfo.FullName}");
                    break;

                case EndCode.CLOSE:
                    Console.WriteLine($"Close Debug Text : {FileInfo.FullName}");
                    break;
            }

            return result;
        }
    }

#if UNITY_EDITOR

    public partial class DebugTextWriter
    {
        public EndCode AppendWithUnityDebug(Func<string> content, int startLine, int endLine)
        {
            var result = Append(content, startLine, endLine);

            switch (result)
            {
                case EndCode.OPEN:
                    Debug.Log($"Open Debug Text  : {FileInfo.FullName}");
                    break;

                case EndCode.CLOSE:
                    Debug.Log($"Close Debug Text : {FileInfo.FullName}");
                    break;
            }

            return result;
        }
    }

#endif
}