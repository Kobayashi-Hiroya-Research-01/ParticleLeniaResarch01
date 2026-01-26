using System;
using System.IO;

#if UNITY_EDITOR

using UnityEngine;

#endif

namespace Assets.ParticleLeniaResarch01.Analysis
{
    public class DebugTextWriter
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

        public void Close()
        {
            if (writer != null)
            {
                writer.Close();
                Log($"Close Debug Text : {FileInfo.FullName}");
            }
        }

        public EndCode AppendLine(Func<string> content, int startLine, int endLine, string lineSeparation = " : ")
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
                var contentText = $"{Line,5}{lineSeparation}{content()}\n";
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
                writer = null;

                return EndCode.CLOSE;
            }
            else
            {
                return EndCode.WRITERING;
            }
        }

        public EndCode AppendLineWithLog(Func<string> content, int startLine, int endLine, string lineSeparation = " : ")
        {
            if (killed) return EndCode.NOT_OPEN;

            var result = AppendLine(content, startLine, endLine, lineSeparation);
            string text = result switch
            {
                EndCode.OPEN => $"Open Debug Text  : {FileInfo.FullName}",
                EndCode.CLOSE => $"Close Debug Text : {FileInfo.FullName}",
                _ => null,
            };

            if (text != null) Log(text);

            return result;
        }

        private void Log(string text)
        {
#if UNITY_EDITOR
            Debug.Log(text);
#else
            Console.WriteLine(text);
#endif
        }
    }
}