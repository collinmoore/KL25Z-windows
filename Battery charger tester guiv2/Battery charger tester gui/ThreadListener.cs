using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battery_charger_tester_gui
{
    interface ThreadListener
    {
        // called by a thread when the thread needs to update richTextBox
        void updateRichTextBox1(String message);
    }
}
