using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GreyMagic;
using System.Threading;
using System.Media;
using System.Runtime.InteropServices;

namespace FishBot
{
    public partial class MainWindow : Form
    {
        private bool Fish = false;
        private IntPtr FishingBobber;
        private IntPtr FirstObj;
        private int Caught = 0;
        static Hook wowHook;
        static Lua lua;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
        }

        public static MainWindow Instance { get; private set; }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormClosing += MainWindow_FormClosing;

            try
            {
                Log.Write("Attempting to connect to running WoW.exe process...", Color.Black);

                var proc = System.Diagnostics.Process.GetProcessesByName("WoW").FirstOrDefault();

                while (proc == null)
                {
                    MessageBox.Show("Please open WoW, and login, and select your character before using the bot.", "FishBot", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    proc = System.Diagnostics.Process.GetProcessesByName("WoW").FirstOrDefault();
                }

                wowHook = new Hook(proc);
                wowHook.InstallHook();
                lua = new Lua(wowHook);

                Log.Write("Connected to process with ID = " + proc.Id, Color.Black);

                textBox1.Text = wowHook.Memory.ReadString(Offsets.PlayerName, Encoding.UTF8, 512, true);

                Log.Write("Base Address = " + wowHook.Process.BaseOffset().ToString("X"));

                Log.Write("Target GUID = " + wowHook.Memory.Read<UInt64>(Offsets.TargetGUID, true));

                IntPtr objMgr = wowHook.Memory.Read<IntPtr>(Offsets.CurMgrPointer, true);
                IntPtr curObj = wowHook.Memory.Read<IntPtr>(IntPtr.Add(objMgr, (int)Offsets.FirstObjectOffset), false);

                FirstObj = curObj;

                Log.Write("First object located @ memory location 0x" + FirstObj.ToString("X"), Color.Black);

                //Thread mouseOver = new Thread(delegate() 
                //    { 
                //        for (;;)
                //        {
                //            Log.Write("MouseOverGUID = " + wowHook.Memory.Read<UInt64>(Offsets.MouseOverGUID, false).ToString("X"));
                //            Thread.Sleep(1000);
                //        }
                //    });
                //mouseOver.Start();

                //lua.DoString("DoEmote('dance')");
                
                Log.Write("Click 'Fish' to begin fishing.", Color.Green);
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message, Color.Red);
            }
        }

        void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            wowHook.DisposeHooking();
        }

        public static bool IsFishing = false;

        public static List<UInt64> lastBobberGuid;

        private void cmdFish_Click(object sender, EventArgs e)
        {
            lastBobberGuid = new List<UInt64>();

            cmdStop.Enabled = true;
            cmdFish.Enabled = false;

            SystemSounds.Asterisk.Play();

            Fish = !Fish;

            while (Fish)
            {
                try
                {
                    Application.DoEvents();

                    if (!IsFishing)
                    {
                        Log.Write("Fishing...", Color.Black);
                        lua.DoString(string.Format("CastSpellByName(\"{0}\")", "Fishing"));
                        Thread.Sleep(200); // Give the lure a chance to be placed in the water before we start scanning for it
                        // 200 ms is a good length, most people play with under that latency
                        IsFishing = true;
                    }

                    var curObj = FirstObj;
                    var nextObj = curObj;

                    while (curObj.ToInt64() != 0 && (curObj.ToInt64() & 1) == 0)
                    {
                        int type = wowHook.Memory.Read<int>(curObj + Offsets.Type, false);
                        var cGUID = wowHook.Memory.Read<UInt64>(curObj + Offsets.LocalGUID, false);

                        //if (cGUID == )

                        if (lastBobberGuid.Count == 5)  // Only keep the last 5 bobber GUID's (explained below * )
                        {
                            lastBobberGuid.RemoveAt(0);
                            lastBobberGuid.TrimExcess();
                        }

                        if ((type == 5) && (!lastBobberGuid.Contains(cGUID)))   // 5 = Game Object, and ensure that we not finding a bobber we already clicked
                        {                                                       // * wow likes leaving the old bobbers in the game world for a while
                            var Name = wowHook.Memory.ReadString(
                                wowHook.Memory.Read<IntPtr>(
                                    wowHook.Memory.Read<IntPtr>(curObj + Offsets.ObjectName1, false) + Offsets.ObjectName2, false
                                    ),
                                    Encoding.UTF8, 50, false
                                );

                            if (Name == "Fishing Bobber")
                            {
                                FishingBobber = curObj;

                                byte bobberState = wowHook.Memory.Read<byte>(curObj + Offsets.BobberState, false);

                                if (bobberState == 1)  // Fish has been caught
                                {
                                    Caught++;
                                    textBox2.Text = Caught.ToString();

                                    Log.Write("Caught something, hopefully a fish!", Color.Black);
                    
                                    wowHook.Memory.Write<UInt64>(Offsets.MouseOverGUID, cGUID, false);
                                    Thread.Sleep(50);

                                    lua.RightClickObject((uint)curObj, 1);

                                    lastBobberGuid.Add(cGUID);
                                    Thread.Sleep(200);
                                                                        
                                    IsFishing = false;
                                    break;
                                }
                            }
                        }

                        nextObj = wowHook.Memory.Read<IntPtr>(IntPtr.Add(curObj, (int)Offsets.NextObjectOffset));
                        if (nextObj == curObj)
                            break;
                        else
                            curObj = nextObj;
                    }
                }
                catch (Exception ex)
                {                    
                    Log.Write(ex.Message, Color.Red);                    
                }
            }
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            cmdStop.Enabled = false;
            cmdFish.Enabled = true;

            SystemSounds.Asterisk.Play();
            Fish = false;
        }
    }
}
