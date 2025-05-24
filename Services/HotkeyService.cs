using OCR.Models; // For AppSettings
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input; // For Key and ModifierKeys
using System.Windows.Interop;
using Application = System.Windows.Application; // To resolve ambiguity
using MessageBox = System.Windows.MessageBox; // To resolve ambiguity

namespace OCR.Services
{
    public class HotkeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT_WINAPI = 0x0001;
        private const uint MOD_CONTROL_WINAPI = 0x0002;
        private const uint MOD_SHIFT_WINAPI = 0x0004;
        // private const uint MOD_WIN_WINAPI = 0x0008; // Windows Key, not typically used for app hotkeys

        private IntPtr _windowHandle;
        private HwndSource _hwndSource;
        private readonly Dictionary<int, Action> _registeredHotkeys = new Dictionary<int, Action>();
        private int _currentHotkeyId = 9000; // Start ID for hotkeys

        public HotkeyService()
        {
            // 获取主窗口句柄的最佳时机是在主窗口 Loaded 事件之后
            // 或者在需要时通过 Application.Current.MainWindow 获取
        }

        private void EnsureWindowHandle()
        {
            if (_windowHandle == IntPtr.Zero)
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    _windowHandle = new WindowInteropHelper(mainWindow).EnsureHandle();
                    _hwndSource = HwndSource.FromHwnd(_windowHandle);
                    _hwndSource?.AddHook(WndProc);
                }
                else
                {
                    throw new InvalidOperationException("无法获取主窗口句柄来注册快捷键。主窗口可能尚未加载。");
                }
            }
        }

        public bool RegisterHotkey(ModifierKeys modifiers, Key key, Action action)
        {
            EnsureWindowHandle();
            if (_windowHandle == IntPtr.Zero) return false;

            uint fsModifiers = 0;
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) fsModifiers |= MOD_CONTROL_WINAPI;
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) fsModifiers |= MOD_SHIFT_WINAPI;
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) fsModifiers |= MOD_ALT_WINAPI;

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            if (vk == 0) return false; // Invalid key

            int hotkeyId = _currentHotkeyId++; // Assign a unique ID

            if (RegisterHotKey(_windowHandle, hotkeyId, fsModifiers, vk))
            {
                _registeredHotkeys[hotkeyId] = action;
                return true;
            }
            System.Diagnostics.Debug.WriteLine($"注册快捷键失败: Mod={fsModifiers}, VK={vk}, ID={hotkeyId}");
            return false;
        }

        public bool UnregisterHotkey(ModifierKeys modifiers, Key key)
        {
            // This unregistration method is problematic as it requires knowing the ID.
            // It's better to unregister all or by ID.
            // For simplicity, we'll unregister all if specific unregistration is needed and ID isn't tracked per key combination.
            // Or, store a mapping of (modifiers, key) -> id if fine-grained unregistration is critical.
            // For now, let's assume we unregister based on finding an ID associated with an action, or clear all.
            // The provided error log implies MainWindow tries to unregister.
            // We will change MainWindow to call UnregisterAllHotkeys instead for simplicity or an improved Unregister by ID.
            
            // Let's find the ID to unregister. This is inefficient.
            // A better approach is to have UnregisterAllHotkeys() or pass the ID.
            // Since MainWindow.xaml.cs tries to call this with modifiers and key,
            // we'll try to find a match, but it's not robust.
            
            // For now, this method won't be directly used by MainWindow, it will call UnregisterAllHotkeys.
            // If a specific unregister is needed, we must store the ID with the (modifiers, key) combination.
            return false; // Mark as not implemented robustly
        }
        
        public void UnregisterAllHotkeys()
        {
            if (_windowHandle == IntPtr.Zero) return;

            foreach (var id in _registeredHotkeys.Keys.ToList()) // ToList to avoid modification during iteration
            {
                UnregisterHotKey(_windowHandle, id);
            }
            _registeredHotkeys.Clear();
            System.Diagnostics.Debug.WriteLine("所有快捷键已注销。");
        }


        public bool UpdateHotkeyFromSettings()
        {
            UnregisterAllHotkeys(); // Unregister all existing hotkeys

            var settings = AppSettings.Load();
            ModifierKeys currentModifiers = ModifierKeys.None;
            if (settings.Hotkey.Ctrl) currentModifiers |= ModifierKeys.Control;
            if (settings.Hotkey.Shift) currentModifiers |= ModifierKeys.Shift;
            if (settings.Hotkey.Alt) currentModifiers |= ModifierKeys.Alt;

            Key currentKey = Key.None;
            try
            {
                if (!string.IsNullOrEmpty(settings.Hotkey.Key))
                {
                    currentKey = (Key)Enum.Parse(typeof(Key), settings.Hotkey.Key, true);
                }
            }
            catch
            {
                MessageBox.Show($"无法解析快捷键 '{settings.Hotkey.Key}'。请在设置中重新配置。", "快捷键错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (currentKey != Key.None && currentModifiers != ModifierKeys.None)
            {
                Action screenshotAction = null;
                var mainWindowInstance = Application.Current.MainWindow as MainWindow;
                if (mainWindowInstance != null)
                {
                    screenshotAction = new Action(mainWindowInstance.TakeScreenshotActionForHotkey); // Create delegate instance
                }
                
                if (screenshotAction != null)
                {
                    if (RegisterHotkey(currentModifiers, currentKey, screenshotAction))
                    {
                        System.Diagnostics.Debug.WriteLine($"快捷键已更新并注册: {currentModifiers} + {currentKey}");
                        return true;
                    }
                    else
                    {
                         MessageBox.Show($"更新并注册快捷键 {currentModifiers} + {currentKey} 失败。", "快捷键错误", MessageBoxButton.OK, MessageBoxImage.Error);
                         return false;
                    }
                }
                else
                {
                      MessageBox.Show($"无法找到截图操作来重新绑定快捷键。", "快捷键错误", MessageBoxButton.OK, MessageBoxImage.Error);
                      return false;
                }
            }
            else if (currentKey == Key.None && currentModifiers == ModifierKeys.None)
            {
                 System.Diagnostics.Debug.WriteLine("快捷键配置为空，不注册任何快捷键。");
                 return true; 
            }
            else
            {
                 MessageBox.Show("快捷键配置无效（需要至少一个修饰键和一个主键）。", "快捷键错误", MessageBoxButton.OK, MessageBoxImage.Error);
                 return false;
            }
        }


        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                if (_registeredHotkeys.TryGetValue(hotkeyId, out Action action))
                {
                    action?.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterAllHotkeys();
            _hwndSource?.RemoveHook(WndProc);
            _hwndSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}