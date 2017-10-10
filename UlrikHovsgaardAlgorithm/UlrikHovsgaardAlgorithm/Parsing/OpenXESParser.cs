using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.deckfour.xes.model;
using org.deckfour.xes.extension.std;
using org.deckfour.xes.@in;
using java.io;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.Parsing
{
    public class OpenXESParser
    {
        public static XLog OpenLog(string inputLogFileName)
        {
            XLog log = null;

            if (inputLogFileName.ToLower().Contains("mxml.gz"))
            {
                XMxmlGZIPParser parser = new XMxmlGZIPParser();
                if (parser.canParse(new File(inputLogFileName)))
                {
                    try
                    {
                        log = (XLog)parser.parse(new File(inputLogFileName)).get(0);
                    }
                    catch (Exception e)
                    {
                        //e.printStackTrace();
                    }
                }
            }
            else if (inputLogFileName.ToLower().Contains("mxml") ||
                     inputLogFileName.ToLower().Contains("xml"))
            {
                XMxmlParser parser = new XMxmlParser();
                if (parser.canParse(new File(inputLogFileName)))
                {
                    try
                    {
                        log = (XLog)parser.parse(new File(inputLogFileName)).get(0);
                    }
                    catch (Exception e)
                    {
                        //e.printStackTrace();
                    }
                }
            }

            if (inputLogFileName.ToLower().Contains("xes.gz"))
            {
                XesXmlGZIPParser parser = new XesXmlGZIPParser();
                if (parser.canParse(new File(inputLogFileName)))
                {
                    try
                    {
                        log = (XLog)parser.parse(new File(inputLogFileName)).get(0);
                    }
                    catch (Exception e)
                    {
                        //e.printStackTrace();
                    }
                }
            }
            else if (inputLogFileName.ToLower().Contains("xes"))
            {
                XesXmlParser parser = new XesXmlParser();
                if (parser.canParse(new File(inputLogFileName)))
                {
                    try
                    {
                        //System.Diagnostics.Debug.WriteLine(parser.parse(new File(inputLogFileName)));
                        log = (XLog)parser.parse(new File(inputLogFileName)).get(0);
                    }
                    catch (Exception e)
                    {
                        //e.printStackTrace();
                        Debug.WriteLine(e.Message);
                    }
                }
            }

            if (log == null)
                throw new Exception("Oops ...");

            return log;
        }

        public string ParseLogTest(XLog log)
        {
            var result = "";
            foreach (XTrace trace in log.toArray())
            {
                foreach (XEvent e in trace.toArray())
                {
                    String activityName = XConceptExtension.instance().extractName(e);
                    result += activityName + Environment.NewLine;
                }
            }
            return result;
        }

        public static Log ParseXesToOurLog(string pathToFile)
        {
            var xLog = OpenLog(pathToFile);

            var log = new Log();

            foreach (XTrace xTrace in xLog.toArray())
            {
                var trace = new LogTrace();
                foreach (XEvent e in xTrace.toArray())
                {
                    string activityName = XConceptExtension.instance().extractName(e);
                    var ev = new LogEvent(activityName, activityName);
                    log.Alphabet.Add(ev); // HashSet-usage

                    trace.Add(ev);
                }
                log.AddTrace(trace);
            }

            return log;
        }
    }
}
