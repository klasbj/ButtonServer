/*
 * Copyright (c) 2013, Klas Björkqvist
 * See COPYING.txt for license information
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SlimDX.DirectInput;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Threading;

namespace ButtonServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Thread serverThread;
        DispatcherTimer timer = new DispatcherTimer();

        List<Joystick> sticks = new List<Joystick>();
        List<JoystickState> stickstate = new List<JoystickState>();

        int joyid = -1;
        int button = 0;
        
        public MainWindow()
        {
            InitializeComponent();

            this.LoadJoysticks();

            if (Properties.Settings.Default.LastFile != "")
            {
                try
                {
                    Command.Load(Properties.Settings.Default.LastFile);
                }
                catch { }
            }

            dataGrid1.ItemsSource = Command.Commands;
            dataGrid1.CanUserAddRows = false;

            // start the server
            serverThread = new Thread(new ThreadStart(Server.Run));
            serverThread.Start();
        }

        private void LoadJoysticks() {
            DirectInput di = new DirectInput();
            List<DeviceInstance> devices = new List<DeviceInstance>();
            
            devices.AddRange(di.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly));

            foreach (DeviceInstance i in devices) {
                if (i.Type == DeviceType.Joystick) {
                    Joystick j = new Joystick(di, i.InstanceGuid);
                    j.Acquire();
                    sticks.Add(j);
                    stickstate.Add(j.GetCurrentState());
                }
            }

            // set up a timer to poll joystick state at 10Hz
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            int ljoyid = -1;
            int lbutton = 0;
            for (int j = 0; j < sticks.Count; j++)
            {
                JoystickState s = sticks[j].GetCurrentState();
                bool[] buttons = s.GetButtons();
                bool[] oldbuttons = stickstate[j].GetButtons();
                bool any = false;

                stickstate[j] = s;

                StringBuilder sb = new StringBuilder();
                sb.Append(sticks[j].Information.InstanceName);
                sb.Append(": ");

                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != oldbuttons[i])
                    {
                        ljoyid = j;
                        lbutton = i;

                        if (buttons[i])
                            sb.Append(" P");
                        else
                            sb.Append(" R");
                        sb.Append(i);
                        any = true;

                        foreach (var cmd in Command.Commands.Where(
                            x => x.Id == sticks[ljoyid].Information.InstanceGuid &&
                                 x.Button == lbutton))
                        {
                            cmd.Execute(buttons[i]);
                        }

                        

                    }
                }

                sb.Append("\n");
                if (any)
                {
                    /* do do do do */
                }
            }
            if (ljoyid >= 0)
            {
                joyid = ljoyid;
                button = lbutton;
                input_text.Text = sticks[ljoyid].Information.InstanceName + " : " + button;
            }
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Open_click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "ButtonServer Xml (*.xml)|*.xml";
            bool? r = ofd.ShowDialog(this);
            if (r.HasValue && r.Value)
            {
                try
                {
                    Command.Load(ofd.FileName);
                    dataGrid1.Items.Refresh();
                    Properties.Settings.Default.LastFile = ofd.FileName;
                }
                catch (Exception err)
                {
                    MessageBox.Show("Unable to save file.\n" + err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Save_click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.AddExtension = true;
            sfd.DefaultExt = ".xml";
            sfd.Filter = "ButtonServer Xml (*.xml)|*.xml";
            bool? r = sfd.ShowDialog(this);
            if (r.HasValue && r.Value)
            {
                try
                {
                    Command.Store(sfd.FileName);
                    Properties.Settings.Default.LastFile = sfd.FileName;
                }
                catch (Exception err)
                {
                    MessageBox.Show("Unable to save file.\n" + err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.timer.Stop();
            foreach (var i in sticks)
            {
                i.Unacquire();
            }

            serverThread.Abort();

            Properties.Settings.Default.Save();
        }

        private void addBtn_Click(object sender, RoutedEventArgs e)
        {
            if (joyid >= 0) {
                Command.Commands.Add(new Command(
                    sticks[joyid].Information.InstanceGuid,
                    sticks[joyid].Information.InstanceName,
                    button));
                dataGrid1.Items.Refresh();
            }
        }

        private void Print_Commands_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cmd in Command.Commands)
            {
                Console.WriteLine(cmd.Joyname + "." + cmd.Button + ": '"
                    + cmd.Down_Command + "' '" + cmd.Up_Command + "'");
            }
            
        }

        private void dataGrid1_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            (sender as DataGrid).CommitEdit();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Command.Commands.Clear();
            dataGrid1.Items.Refresh();
        }

        private void DataGridCheckBoxColumn_Selected(object sender, RoutedEventArgs e)
        {
            DataGrid dg = (sender as DataGrid);
            Console.WriteLine("" + dg.SelectedIndex);
        }

        private void Remove_Selected_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelected();
        }

        private void RemoveSelected()
        {
            DataGrid dg = dataGrid1;
            if (dg.SelectedIndex >= 0)
            {
                Command.Commands.RemoveAt(dg.SelectedIndex);
                dg.Items.Refresh();
            }
        }

    }
}
