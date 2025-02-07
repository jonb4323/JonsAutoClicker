using Microsoft.VisualBasic;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoClickerFinal {
    public partial class Form1 : Form {
        public bool isOn = false;
        private System.Windows.Forms.Timer clickTimer;
        private bool pickLocations = false;
        private bool useCurrentLocation = true;
        private int clickCount = 0;
        private Point location1, location2;
        private int clickToggle = 0;

        // Import mouse event functions
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;

        //GlobalMouse hook 
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc mouseProc;
        private const int WH_MOUSE_LL = 14;
        private IntPtr mouseHook = IntPtr.Zero;
        
        // Hotkey registration
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000; // Unique ID for the hotkey
        private const int MOD_NONE = 0x0000; // No modifier key
        private Keys selectedHotkey = Keys.F5; // Default hotkey

        public Form1() {
            InitializeComponent();
            this.MaximizeBox = false;

            // Initialize the timer
            clickTimer = new System.Windows.Forms.Timer();
            clickTimer.Tick += ClickTimer_Tick;

            // Set default values for interval boxes
            textBox1.Text = "0";
            textBox2.Text = "0";
            textBox3.Text = "0";
            textBox4.Text = "0";

            // Attach event handlers to ensure only positive integers are entered
            textBox1.KeyPress += TextBox_KeyPress;
            textBox2.KeyPress += TextBox_KeyPress;
            textBox3.KeyPress += TextBox_KeyPress;
            textBox4.KeyPress += TextBox_KeyPress;

            // Reset empty fields to "0" when losing focus
            textBox1.Leave += TextBox_Leave;
            textBox2.Leave += TextBox_Leave;
            textBox3.Leave += TextBox_Leave;
            textBox4.Leave += TextBox_Leave;
        }

        // Ensure only positive integers can be entered
        private void TextBox_KeyPress(object sender, KeyPressEventArgs e) {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) {
                e.Handled = true; // Block non-numeric input
            }
        }

        // Reset empty fields to "0" when losing focus
        private void TextBox_Leave(object sender, EventArgs e) {
            TextBox tb = sender as TextBox;
            if (string.IsNullOrWhiteSpace(tb.Text)) {
                tb.Text = "0";
            }
        }

        // Mouse click event
        private void ClickTimer_Tick(object sender, EventArgs e) {
            if (useCurrentLocation) {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }
            else {
                Point target = (clickToggle % 2 == 0) ? location1 : location2;
                Cursor.Position = target;
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                clickToggle++;
            }
        }

        // Convert textbox values to a total interval in milliseconds
        private int GetIntervalFromTextboxes() {
            int hours = int.TryParse(textBox1.Text, out int h) ? h : 0;
            int minutes = int.TryParse(textBox2.Text, out int m) ? m : 0;
            int seconds = int.TryParse(textBox3.Text, out int s) ? s : 0;
            int milliseconds = int.TryParse(textBox4.Text, out int ms) ? ms : 0;

            int totalMilliseconds = (hours * 3600000) + (minutes * 60000) + (seconds * 1000) + milliseconds;

            // Ensure minimum interval is at least 1 millisecond
            return Math.Max(totalMilliseconds, 1);
        }

        // Handle hotkey press
        protected override void WndProc(ref Message m) {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY) {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID) {
                    isOn = !isOn;
                    label13.Text = isOn ? "ON" : "OFF";

                    if (isOn) {
                        int interval = GetIntervalFromTextboxes();
                        clickTimer.Interval = interval;
                        clickTimer.Start();
                    }
                    else {
                        clickTimer.Stop();
                    }
                }
            }
            base.WndProc(ref m);
        }

        // Register the hotkey when the form loads
        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_NONE, (int)selectedHotkey);
        }

        // Unregister the hotkey when the form closes
        protected override void OnFormClosed(FormClosedEventArgs e) {
            base.OnFormClosed(e);
            UnregisterHotKey(this.Handle, HOTKEY_ID);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e) {
            this.KeyDown -= Form1_KeyDown; // Remove the event handler after key press

            Keys newHotkey = e.KeyCode;

            // Unregister the previous hotkey
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            // Register the new hotkey
            if (RegisterHotKey(this.Handle, HOTKEY_ID, MOD_NONE, (int)newHotkey)) {
                selectedHotkey = newHotkey;
                label9.Text = selectedHotkey.ToString();
            }
        }

        private void button3_Click_1(object sender, EventArgs e) {
            this.KeyPreview = true; // Ensure form captures key presses
            label9.Text = "Select a Hotkey".ToString();
            this.KeyDown += Form1_KeyDown;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e) { // pick two coords 
            pickLocations = true;
            useCurrentLocation = false;
            clickCount = 0;
            mouseProc = MouseHookCallback;
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, GetModuleHandle(null), 0);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) { // pick the current location 
            pickLocations = false;
            useCurrentLocation = true;
            label16.Text = "0";
            label18.Text = "0";
            label21.Text = "0";
            label22.Text = "0";
        }
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0 && wParam == (IntPtr)0x201 && useCurrentLocation != true) { // WM_LBUTTONDOWN 
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                if (clickCount == 0) {
                    location1 = new Point(hookStruct.pt.x, hookStruct.pt.y);
                    label16.Text = location1.X.ToString();
                    label18.Text = location1.Y.ToString();
                    clickCount++;
                }
                else if (clickCount == 1) {
                    location2 = new Point(hookStruct.pt.x, hookStruct.pt.y);
                    label21.Text = location2.X.ToString();
                    label22.Text = location2.Y.ToString();
                    clickCount++;
                    UnhookWindowsHookEx(mouseHook);
                }
            }
            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT {
            public int x;
            public int y;
        }
    }
}