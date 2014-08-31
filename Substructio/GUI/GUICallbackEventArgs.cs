using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Substructio.GUI
{
    public abstract class GUICallbackEventArgs : EventArgs
    {
        public GUICallbackEventArgs(string objectName, object caller, string callbackName, string[] arguments)
        {
            Arguments = arguments;
            CallbackName = callbackName;
            Caller = caller;
            ObjectName = objectName;
        }

        public string ObjectName { get; private set; }
        public object Caller { get; private set; }
        public string CallbackName { get; private set; }
        public string[] Arguments { get; private set; }

    }
}
