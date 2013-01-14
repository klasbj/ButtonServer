/*
 * Copyright (c) 2013, Klas Björkqvist
 * See COPYING.txt for license information
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace ButtonServer
{
    [Serializable]
    public class Command
    {
        public static List<Command> Commands = new List<Command>();

        public static void Load(String filename)
        {
            var f = File.OpenRead(filename);

            XmlSerializer xml = new XmlSerializer(typeof(Command[]),
                                                  new Type[] { typeof(Command) });

            Command[] commands;

            try
            {
                commands = (Command[])xml.Deserialize(f);
                Commands.AddRange(commands);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                f.Close();
            }
        }

        public static void Store(String filename)
        {
            var f = File.OpenWrite(filename);
            XmlSerializer xml = new XmlSerializer(typeof(Command[]),
                                                  new Type[] { typeof(Command) });

            Command[] commands = Commands.ToArray();
            try
            {
                xml.Serialize(f, commands);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                f.Close();
            }

            f.Close();
        }

        public Guid Id { get; set; }
        public String Joyname { get; set; }
        public int Button { get; set; }
        public String Down_Command { get; set; }
        public String Up_Command { get; set; }

        public Command() { }

        public Command(Guid id, String joyname, int button, String down, String up)
        {
            this.Id = id;
            this.Joyname = joyname;
            this.Button = button;
            this.Down_Command = down;
            this.Up_Command = up;
        }

        public Command(Guid id, String joyname, int button)
        {
            this.Id = id;
            this.Joyname = joyname;
            this.Button = button;
            this.Down_Command = "";
            this.Up_Command = "";
        }

        public void Execute(bool down)
        {
            if (down)
            {
                Server.EnqueueMessage(Down_Command);
            }
            else
            {
                Server.EnqueueMessage(Up_Command);
            }
        }
    }
}
