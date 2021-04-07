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
using Gee.External.Capstone;
using Gee.External.Capstone.X86;
using System.Collections;

namespace asmtool {


    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    public partial class MainWindow :Window {


        Engine asm2hexEng = new Engine(Architecture.X86, Mode.X32);
        CapstoneX86Disassembler dis = CapstoneDisassembler.CreateX86Disassembler(X86DisassembleMode.Bit32);

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

        private CapstoneX86Disassembler createCapstoneX86Eng(string mode) {
            if (mode == "32bit") {
                return CapstoneDisassembler.CreateX86Disassembler(X86DisassembleMode.Bit32);
            }else if (mode =="64bit") {
                return CapstoneDisassembler.CreateX86Disassembler(X86DisassembleMode.Bit64);
            }
            return null;
        }

        private string getBitMode() {
            return ((ComboBoxItem)bitMode.SelectedItem).Content.ToString();
        }

        private string getArchType() {
            return (string)archType.SelectedItem;
        }

        private string getConvertMode() {
            return ((ComboBoxItem)convertMode.SelectedItem).Content.ToString();
        }
        

        public MainWindow() {
            InitializeComponent();
            archType.ItemsSource = new List<string> { "x86", "AArch64", "ARM", "Hexagon", "Mips", "PowerPC", "Sparc", "SystemZ" };
            archType.SelectedIndex = 0;
        }

        private void updateAsm2Hex() {
            string text = input.Text;

            string s = text.Substring(0, 2);
            if(s == "\\x") {
                input.Text = "";
                result.Text = "输入内容不合法，内容不应该以 \\x 开头";
                return;
            }

            if(asm2hexEng != null) {
                EncodedData data = asm2hexEng.Assemble(text, 0);
                if(data == null) {
                    result.Text = "编码错误";
                    return;
                }
                byte[] bytes = data.Buffer;
                StringBuilder sb = new StringBuilder();
                foreach(var item in bytes) {
                    s = $"\\x{item:x2}";
                    sb.Append(s);
                }
                result.Text = sb.ToString();
            } else {
                MessageBox.Show("汇编引擎未初始化或者初始化出错");
            }
        }

        private void update() {
            string convertMode = getConvertMode();
            if(convertMode == "hex2asm") {
                updateHex2Asm();
            } else {
                updateAsm2Hex();
            }
        }


        private void updateHex2Asm() {
            string text = input.Text;
            int len = text.Length;
            if (len % 4 != 0) {
                result.Text = "编码错误，检查输入是否正确";
                return;
            }

            ArrayList bs = new ArrayList();
            if(dis != null) {
                for(int i = 0; i < len; i+=4) {
                    string s = text.Substring(i+2, 2);
                    try {
                        byte b = Convert.ToByte(s, 16);
                        bs.Add(b);
                    } catch(Exception) {
                        result.Text = "编码错误，检查输入是否正确";
                        return;
                    }
                }
                
                byte[] hexCode = (byte[])bs.ToArray(typeof(byte));
                dis.DisassembleSyntax = DisassembleSyntax.Intel;
                X86Instruction[] instructions = dis.Disassemble(hexCode, 0);
                StringBuilder sb = new StringBuilder();
                foreach(var instruction in instructions) {
                    var address = instruction.Address;
                    string line = $"addr:\t{address}\t{instruction.Mnemonic} {instruction.Operand}\n";
                    sb.Append(line);
                }

                result.Text = sb.ToString();

            } else {
                MessageBox.Show("引擎未初始化或者初始化出错");
            }
        }

        private void convertModeChanged(object sender, SelectionChangedEventArgs e) {
            Dispatcher.InvokeAsync(() => {
                string convertMode = getConvertMode();
                if(convertMode == "asm2hex") {
                    leftDescription.Content = "asm";
                    rightDescription.Content = "hex";
                } else if(convertMode == "hex2asm") {
                    leftDescription.Content = "hex";
                    rightDescription.Content = "asm";
                } else {
                    MessageBox.Show("转换模式错误，请检查转换模式是否错误");
                    return;
                }
                update();
            });
        }

        private void bitModeChanged(object sender, SelectionChangedEventArgs e) {
            Dispatcher.InvokeAsync(() => {
                string bitMode = getBitMode();
                if(asm2hexEng != null) {
                    asm2hexEng.Dispose();
                }
                string type = getArchType();
                asm2hexEng = createAsmEngine(type, bitMode);
                if(asm2hexEng == null) {
                    MessageBox.Show("汇编引擎初始化出错，检查参数！");
                }

                if (bitMode == "32bit") {
                    dis.DisassembleMode = X86DisassembleMode.Bit32;
                } else if (bitMode == "64bit") {
                    dis.DisassembleMode = X86DisassembleMode.Bit64;
                } else {
                    MessageBox.Show("获取位数出错，检查参数！");
                }

                update();
            });
        }

        private void archTypeChanged(object sender, SelectionChangedEventArgs e) {
            Dispatcher.InvokeAsync(() => {
                string archType = getArchType();
                if(asm2hexEng != null) {
                    asm2hexEng.Dispose();
                }
                string mode = getBitMode();
                asm2hexEng = createAsmEngine(archType, mode);
                if(asm2hexEng == null) {
                    MessageBox.Show("汇编引擎初始化出错，检查参数！");
                }
                updateAsm2Hex();
            });
        }

        private void inputTextChanged(object sender, TextChangedEventArgs e) {
            Dispatcher.InvokeAsync(() => {
                update();
            });
        }
    }
}
