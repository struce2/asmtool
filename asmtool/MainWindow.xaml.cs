using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Keystone;

namespace asmtool {


    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    public partial class MainWindow :Window {


        Engine asm2hexEng;

        public Engine createAsmEngine(string architecture, string mode) {
            Mode m = Mode.X32;
            // 目前仅支持 x86 与 x64
            if(mode.ToLower() == "64bit") {
                m = Mode.X64;
            }
            Architecture arch;
            switch(architecture) {
                case "ARM":
                    arch = Architecture.ARM;
                    break;
                case "AArch64":
                    arch = Architecture.ARM64;
                    break;
                case "Mips":
                    arch = Architecture.MIPS;
                    break;
                case "x86":
                    arch = Architecture.X86;
                    break;
                case "PowerPC":
                    arch = Architecture.PPC;
                    break;
                case "Sparc":
                    arch = Architecture.SPARC;
                    break;
                case "SystemZ":
                    arch = Architecture.SYSTEMZ;
                    break;
                case "Hexagon":
                    arch = Architecture.HEXAGON;
                    break;
                default:
                    return null;
            }
            return new Engine(arch, m);
        }

        public MainWindow() {
            InitializeComponent();
            asm2hexEng = new Engine(Architecture.X86, Mode.X32);
            archType.ItemsSource = new List<string> { "x86", "AArch64", "ARM", "Hexagon", "Mips", "PowerPC", "Sparc", "SystemZ" };
            archType.SelectedIndex = 0;
        }

        private void updateData() {
            string text = input.Text;
            if(asm2hexEng != null) {
                EncodedData data = asm2hexEng.Assemble(text, 0);
                if(data == null) {
                    result.Text = "编码错误";
                    return;
                }
                byte[] bytes = data.Buffer;
                StringBuilder sb = new StringBuilder();
                foreach(var item in bytes) {
                    string s = $"\\x{item:x2}";
                    sb.Append(s);
                }
                result.Text = sb.ToString();
            } else {
                MessageBox.Show("汇编引擎未初始化或者初始化出错");
            }
        }

        private void convertModeChanged(object sender, SelectionChangedEventArgs e) {
            ComboBox box = (ComboBox)sender;
            ComboBoxItem item = (ComboBoxItem)box.SelectedItem;
            string convertMode = item.Content.ToString();
        }

        private void bitModeChanged(object sender, SelectionChangedEventArgs e) {
            Dispatcher.InvokeAsync(() => {
                ComboBox box = (ComboBox)sender;
                ComboBoxItem item = (ComboBoxItem)box.SelectedItem;
                string bitMode = item.Content.ToString();
                if(asm2hexEng != null) {
                    asm2hexEng.Dispose();
                }
                string type = (string)archType.SelectedItem;
                asm2hexEng = createAsmEngine(type, bitMode);
                if(asm2hexEng == null) {
                    MessageBox.Show("汇编引擎初始化出错，检查参数！");
                }
                updateData();
            });
        }

        private void archTypeChanged(object sender, SelectionChangedEventArgs e) {
            Dispatcher.InvokeAsync(() => {
                ComboBox box = (ComboBox)sender;
                string archType = (string)box.SelectedItem;
                if(asm2hexEng != null) {
                    asm2hexEng.Dispose();
                }
                string mode = ((ComboBoxItem)bitMode.SelectedItem).Content.ToString();
                asm2hexEng = createAsmEngine(archType, mode);
                if(asm2hexEng == null) {
                    MessageBox.Show("汇编引擎初始化出错，检查参数！");
                }
                updateData();
            });
        }

        private void inputTextChanged(object sender, TextChangedEventArgs e) {
            Dispatcher.InvokeAsync(() => {
                updateData();
            });
        }
    }
}
