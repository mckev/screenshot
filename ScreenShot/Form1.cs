using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ScreenShot
{
    public partial class Form1 : Form
    {
        private string saveLocation;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Prevent the same application to run twice
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                // http://stackoverflow.com/questions/93989/prevent-multiple-instances-of-a-given-app-in-net
                MessageBox.Show("Sorry, only one instance of Screen Shot application is allowed.");
                this.Close();
            }

            // Set form
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            this.MaximizeBox = false;
            this.Text = "Screen Shot";
            notifyIcon1.BalloonTipText = "Screen Shot";
            notifyIcon1.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon1.Text = "Screen Shot";
            // Ref: http://www.codeproject.com/Articles/2099/Adding-tray-icons-and-context-menus
            ContextMenu notifyIconContextMenu = new ContextMenu();
            notifyIconContextMenu.MenuItems.Add(0, new MenuItem("Show", new System.EventHandler(notifyIconShow)));
            notifyIconContextMenu.MenuItems.Add(1, new MenuItem("Exit", new System.EventHandler(notifyIconExit)));
            notifyIcon1.ContextMenu = notifyIconContextMenu;

            // Set save location
            this.saveLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            this.locationLabel.Text = this.saveLocation;

            // Start minimized
            this.WindowState = FormWindowState.Minimized;

            // Hook the keyboard
            KeyboardHook keyboardHook = new KeyboardHook((int)WIN32_API.VK_SCROLL, DoSomething);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // Ref: http://www.codeproject.com/Articles/27599/Minimize-window-to-system-tray
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }
            else if (this.WindowState == FormWindowState.Normal)
            {
                notifyIcon1.Visible = false;
                this.ShowInTaskbar = true;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
        }

        private void notifyIconShow(object sender, System.EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
        }

        private void notifyIconExit(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void DoSomething()
        {
            Bitmap screenshot = ScreenShot.DoScreenShot();
            FlashScreen.DoFlashScreen();

            String filename = String.Format("Screen Shot {0}.png", DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss tt"));
            string filepath = Path.Combine(this.saveLocation, filename);
            screenshot.Save(filepath, ImageFormat.Png);
        }
    }


    class ScreenShot
    {
        public static Bitmap DoScreenShot()
        {
            // Ref: http://stackoverflow.com/questions/158151/how-can-i-save-a-screenshot-directly-to-a-file-in-windows
            WIN32_API.SIZE size;
            size.cx = WIN32_API.GetSystemMetrics(WIN32_API.SM_CXSCREEN);
            size.cy = WIN32_API.GetSystemMetrics(WIN32_API.SM_CYSCREEN);
            IntPtr hDC = WIN32_API.GetDC(WIN32_API.GetDesktopWindow());
            IntPtr hMemDC = WIN32_API.CreateCompatibleDC(hDC);
            IntPtr m_HBitmap = WIN32_API.CreateCompatibleBitmap(hDC, size.cx, size.cy);
            if (m_HBitmap == IntPtr.Zero)
            {
                return null;
            }
            IntPtr hOld = (IntPtr)WIN32_API.SelectObject(hMemDC, m_HBitmap);
            WIN32_API.BitBlt(hMemDC, 0, 0, size.cx, size.cy, hDC, 0, 0, WIN32_API.SRCCOPY);
            WIN32_API.SelectObject(hMemDC, hOld);
            WIN32_API.DeleteDC(hMemDC);
            WIN32_API.ReleaseDC(WIN32_API.GetDesktopWindow(), hDC);
            return System.Drawing.Image.FromHbitmap(m_HBitmap);
        }
    }


    class FlashScreen
    {
        public static void DoFlashScreen()
        {
            // Ref:
            // http://stackoverflow.com/questions/14385838/draw-on-screen-without-form
            // http://stackoverflow.com/questions/2527679/how-to-clean-up-after-myself-when-drawing-directly-to-the-screen
            WIN32_API.SIZE size;
            size.cx = WIN32_API.GetSystemMetrics(WIN32_API.SM_CXSCREEN);
            size.cy = WIN32_API.GetSystemMetrics(WIN32_API.SM_CYSCREEN);
            IntPtr hDC = WIN32_API.GetDC(WIN32_API.GetDesktopWindow());
            Graphics g = Graphics.FromHdc(hDC);
            SolidBrush b = new SolidBrush(Color.White);
            g.FillRectangle(b, new Rectangle(0, 0, size.cx, size.cy));
            g.Dispose();
            WIN32_API.ReleaseDC(WIN32_API.GetDesktopWindow(), hDC);
            WIN32_API.InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
        }
    }


    class KeyboardHook
    {
        private int _vkCode;
        private IntPtr _hookId;
        private Action _callback;

        public KeyboardHook(int vkCode, Action callback)
        {
            // Ref: http://forum.cheatengine.org/viewtopic.php?t=192699&sid=d25bb4a9d48a3518bba28ec63d6510a2
            this._vkCode = vkCode;
            this._callback = callback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                this._hookId = WIN32_API.SetWindowsHookEx(WIN32_API.WH_KEYBOARD_LL, HookCallback, WIN32_API.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WIN32_API.WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                // Console.WriteLine("Key is pressed: " + (Keys)vkCode + " - " + vkCode);

                // React on the defined key
                if (vkCode == this._vkCode)
                {
                    // Ref: http://stackoverflow.com/questions/9931723/passing-a-callback-function-to-another-class
                    _callback();
                }
            }
            return WIN32_API.CallNextHookEx(this._hookId, nCode, wParam, lParam);
        }

        ~KeyboardHook()
        {
            WIN32_API.UnhookWindowsHookEx(this._hookId);
        }
    }


    class WIN32_API
    {
        // For screen shot
        public struct SIZE
        {
            public int cx;
            public int cy;
        }
        public const int SRCCOPY = 13369376;
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

        [DllImport("gdi32.dll")]
        public static extern IntPtr DeleteDC(IntPtr hDc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr DeleteObject(IntPtr hDc);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, int RasterOp);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr ptr);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int abc);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(Int32 ptr);

        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);


        // For keyboard hook
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;
        public const int VK_INSERT = 0X2D;
        public const int VK_SCROLL = 0X91;
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);


        // For flash screen
        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
    }

}
