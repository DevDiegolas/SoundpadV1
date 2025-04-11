using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NAudio.Wave;

namespace Soundpad
{
    public partial class Form1 : Form
    {
        private Dictionary<Keys, string> keySoundMap = new();
        private float volume = 1.0f;
        private int selectedOutputDeviceIndex = -1;

        private IntPtr _hookID = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc? _proc;

        private string? tempPath = null;
        private Button? tempBtn = null;

        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.Size = new Size(700, 600);

            CreateAddSoundButton();
            CreateVolumeControl();
            CreateDeviceSelector();
            LoadSoundButtons();

            _proc = HookCallback;
            _hookID = NativeMethods.SetHook(_proc);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            NativeMethods.UnhookWindowsHookEx(_hookID);
            base.OnFormClosed(e);
        }

        // Hook global para atalhos com teclado
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)0x100) // WM_KEYDOWN
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                if (keySoundMap.ContainsKey(key))
                    PlaySound(keySoundMap[key]);
            }
            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (keySoundMap.ContainsKey(e.KeyCode))
                PlaySound(keySoundMap[e.KeyCode]);
        }

        // Criação do botão "Adicionar Som"
        private void CreateAddSoundButton()
        {
            Button addBtn = new Button
            {
                Text = "Adicionar Som",
                Width = 120,
                Height = 40,
                Location = new Point(10, 10)
            };

            addBtn.Click += (s, e) =>
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "Arquivos de Áudio (*.mp3)|*.mp3"
                };

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string destDir = Path.Combine(Application.StartupPath, "sons");
                    Directory.CreateDirectory(destDir);
                    string destPath = Path.Combine(destDir, Path.GetFileName(ofd.FileName));

                    try
                    {
                        File.Copy(ofd.FileName, destPath, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao copiar o arquivo: " + ex.Message);
                        return;
                    }

                    this.Controls.Clear();
                    CreateAddSoundButton();
                    CreateVolumeControl();
                    CreateDeviceSelector();
                    LoadSoundButtons();
                }
            };

            this.Controls.Add(addBtn);
        }

        // Controle de volume
        private void CreateVolumeControl()
        {
            TrackBar volumeBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 300,
                Value = (int)(volume * 100),
                TickStyle = TickStyle.None,
                Width = 100,
                Location = new Point(140, 15)
            };

            volumeBar.Scroll += (s, e) =>
            {
                volume = volumeBar.Value / 100f;
            };

            this.Controls.Add(volumeBar);
        }

        // Selecionar dispositivo de saída de áudio
        private void CreateDeviceSelector()
        {
            ComboBox deviceSelector = new ComboBox
            {
                Name = "deviceSelector",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Location = new Point(270, 10)
            };

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                deviceSelector.Items.Add($"{i}: {caps.ProductName}");
            }

            deviceSelector.Items.Insert(0, "Selecione o dispositivo de saída");
            deviceSelector.SelectedIndex = 0;
            selectedOutputDeviceIndex = -1;

            deviceSelector.SelectedIndexChanged += (s, e) =>
            {
                selectedOutputDeviceIndex = deviceSelector.SelectedIndex > 0
                    ? deviceSelector.SelectedIndex - 1
                    : -1;
            };

            this.Controls.Add(deviceSelector);
        }

        // Carrega os botões de som com atalhos
        private void LoadSoundButtons()
        {
            string folderPath = Path.Combine(Application.StartupPath, "sons");

            if (!Directory.Exists(folderPath))
                return;

            var files = Directory.GetFiles(folderPath, "*.mp3");

            int x = 10;
            int y = 60;
            int btnWidth = 180;
            int btnHeight = 40;
            int spacing = 10;

            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                // Botão de reprodução
                Button playBtn = new Button
                {
                    Text = fileName,
                    Width = btnWidth,
                    Height = btnHeight,
                    Location = new Point(x, y)
                };
                playBtn.Click += (s, e) => PlaySound(file);
                this.Controls.Add(playBtn);

                // Botão de atalho
                Button keyBtn = new Button
                {
                    Text = "Atalho",
                    Width = 60,
                    Height = btnHeight,
                    Location = new Point(x + btnWidth + 10, y)
                };
                keyBtn.Click += (s, e) =>
                {
                    MessageBox.Show("Pressione uma tecla para configurar...");
                    this.KeyDown += SetKeyShortcut;
                    tempPath = file;
                    tempBtn = keyBtn;
                };
                this.Controls.Add(keyBtn);

                // Botão de remover som
                Button removeBtn = new Button
                {
                    Text = "X",
                    Width = 40,
                    Height = btnHeight,
                    Location = new Point(x + btnWidth + 80, y)
                };
                removeBtn.Click += (s, e) =>
                {
                    File.Delete(file);
                    this.Controls.Clear();
                    CreateAddSoundButton();
                    CreateVolumeControl();
                    CreateDeviceSelector();
                    LoadSoundButtons();
                };
                this.Controls.Add(removeBtn);

                // Botão para remover o atalho (⛔)
                Button clearKeyBtn = new Button
                {
                    Text = "⛔",
                    Width = 40,
                    Height = btnHeight,
                    Location = new Point(x + btnWidth + 130, y)
                };
                clearKeyBtn.Click += (s, e) =>
                {
                    var keysToRemove = new List<Keys>();
                    foreach (var pair in keySoundMap)
                    {
                        if (pair.Value == file)
                            keysToRemove.Add(pair.Key);
                    }
                    foreach (var key in keysToRemove)
                    {
                        keySoundMap.Remove(key);
                    }
                    keyBtn.Text = "Atalho";
                };
                this.Controls.Add(clearKeyBtn);

                y += btnHeight + spacing;
            }
        }

        // Define o atalho do som
        private void SetKeyShortcut(object? sender, KeyEventArgs e)
        {
            if (tempPath != null && tempBtn != null)
            {
                var keysToRemove = new List<Keys>();
                foreach (var pair in keySoundMap)
                {
                    if (pair.Value == tempPath)
                        keysToRemove.Add(pair.Key);
                }
                foreach (var key in keysToRemove)
                {
                    keySoundMap.Remove(key);
                }

                keySoundMap[e.KeyCode] = tempPath;
                tempBtn.Text = e.KeyCode.ToString();
                tempPath = null;
                tempBtn = null;
                this.KeyDown -= SetKeyShortcut;
            }
        }

        // Reproduz o som
        private void PlaySound(string filePath)
        {
            try
            {
                var audioFile = new AudioFileReader(filePath)
                {
                    Volume = volume
                };

                var outputDevice = new WaveOutEvent
                {
                    DeviceNumber = selectedOutputDeviceIndex
                };

                outputDevice.Init(audioFile);
                outputDevice.Play();

                outputDevice.PlaybackStopped += (s, a) =>
                {
                    outputDevice.Dispose();
                    audioFile.Dispose();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao reproduzir som: " + ex.Message);
            }
        }
    }

    // Native methods (hooks de teclado)
    internal static class NativeMethods
    {
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            return SetWindowsHookEx(13, proc, GetModuleHandle(curModule.ModuleName!), 0);
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
