using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishBot
{
    public class Lua
    {
        private bool codecaveCreated;
        private IntPtr codecave;
        private readonly Hook _wowHook;
        public Lua(Hook wowHook)
        {
            _wowHook = wowHook;
        }

        public void DoString(string command)
        {
            if (_wowHook.Installed)
            {
                // Allocate memory
                IntPtr doStringArgCodecave = _wowHook.Memory.AllocateMemory(Encoding.UTF8.GetBytes(command).Length + 1);
                // Write value:
                _wowHook.Memory.WriteBytes(doStringArgCodecave, Encoding.UTF8.GetBytes(command));

                var asm = new[] 
		        {
			        "mov ecx, " + doStringArgCodecave,
			        "mov edx, " + doStringArgCodecave,
			        "call " + ((uint) Offsets.FrameScript__Execute + _wowHook.Process.BaseOffset()),  // Lua_DoString   
			        "retn",    
		        };
                
                // Inject
                _wowHook.InjectAndExecute(asm);
                // Free memory allocated 
                _wowHook.Memory.FreeMemory(doStringArgCodecave);
            }
        }

        void AutoLoot()
        {
            if (codecaveCreated) return;

            codecaveCreated = true;

            codecave = _wowHook.Memory.AllocateMemory(512);
            _wowHook.Memory.Asm.Clear();
            _wowHook.Memory.Asm.AddLine("call " + (uint)0x4C1FA0);
            _wowHook.Memory.Asm.AddLine("retn");
            _wowHook.Memory.Asm.InjectAndExecute((uint)codecave);
        }

        public void RightClickObject(uint _curObject, int autoLoot)
        {
            try
            {
                AutoLoot();
                System.Threading.Thread.Sleep(50);
                
                _wowHook.Memory.Asm.Clear();
                _wowHook.Memory.Asm.AddLine("push {0}", autoLoot);
                _wowHook.Memory.Asm.AddLine("mov ECX, " + (uint)_curObject);
                _wowHook.Memory.Asm.AddLine("call " + (uint)0x005F8660);
                _wowHook.Memory.Asm.AddLine("retn");
                _wowHook.Memory.Asm.InjectAndExecute((uint)codecave);
            }
            catch (Exception ex)
            {
                Log.Err(ex.Message);
            }
        }
    }
}
