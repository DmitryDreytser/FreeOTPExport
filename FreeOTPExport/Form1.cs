using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Collections;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace FreeOTPExport
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public class OTPnode
        {
            public string name;
            public string algorithm;
            public byte[] secret = new byte[20];
            public string HashCode;
            public string issuerExt;
            public string issuerInt;
            public string label;
            public string counter;
            public int digits;
            public int period;
            public string type;
            public string uri
            {
                get
                {
                    return string.Format("otpauth://{0}/{1}?secret={2}&algorithm={3}&digits={4}&period={5}", type.ToLower(), name.Replace("@","%40"), HashCode, algorithm, digits, period);
                }
            }

            public override string ToString()
            {
                return name;
            }


        }

        private void button1_Click(object sender, EventArgs e)
        {
            FileDialog FD = new OpenFileDialog();
            FD.FileName = "tokens.xml";
            FD.Filter = "FreeOTP XML|tokens.xml";
            if (FD.ShowDialog() != DialogResult.OK)
                return;
            XmlDocument tokens = new XmlDocument();
            tokens.Load(FD.FileName);
            string node = "";

            List<OTPnode> Map = new List<OTPnode>();
            foreach (XmlNode token in tokens.DocumentElement.GetElementsByTagName("string"))
            {
                string nodeName = token.Attributes["name"].InnerText;
                if (nodeName == "tokenOrder")
                    continue;

                OTPnode item = new OTPnode();
                item.name = nodeName;
                string[] nodeParams = token.InnerText.Replace("{", "").Replace("}", "").Split(new string[] { ",\"" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string nodeParam in nodeParams)
                {
                    string ParamName = nodeParam.Replace("\"", "").Split(':')[0];
                    string ParamValue = nodeParam.Replace("\"", "").Split(':')[1];
                    switch (ParamName)
                    {
                        case "algo":
                            item.algorithm = ParamValue;
                            break;
                        case "type":
                            item.type = ParamValue;
                            break;
                        case "issuerExt":
                            item.issuerExt = ParamValue;
                            break;
                        case "issuerInt":
                            item.issuerInt = ParamValue;
                            break;
                        case "label":
                            item.label = ParamValue;
                            break;
                        case "counter":
                            item.counter = ParamValue;
                            break;
                        case "digits":
                            item.digits = int.Parse(ParamValue);
                            break;
                        case "period":
                            item.period = int.Parse(ParamValue);
                            break;
                        case "secret":
                            string[] secretBytes = ParamValue.Replace("]", "").Replace("[", "").Split(',');
                            int counter = 0;
                            foreach (string secretByte in secretBytes)
                            {
                                item.secret[counter] = (byte)sbyte.Parse(secretByte);
                                counter++;
                            }
                            Array.Resize(ref item.secret, counter);
                            item.HashCode = Base32.ToBase32String(item.secret).ToUpper();
                            break;
                    }
                }
                Map.Add(item);
                listBox1.Items.Add(item);


            }


        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox1.Text = ((OTPnode)listBox1.SelectedItem).uri;

            QRCodeWriter qrEncode = new QRCodeWriter(); //создание QR кода
            //Dictionary<EncodeHintType, object> hints = new Dictionary<EncodeHintType, object>();    //для колекции поведений
            //hints.Add(EncodeHintType.CHARACTER_SET, "");   //добавление в коллекцию кодировки utf-8
            BitMatrix qrMatrix = qrEncode.encode(   //создание матрицы QR
                textBox1.Text,                 //кодируемая строка
                BarcodeFormat.QR_CODE,  //формат кода, т.к. используется QRCodeWriter применяется QR_CODE
                300,                    //ширина
                300                    //высота
                );                 //применение колекции поведений

            BarcodeWriter qrWrite = new BarcodeWriter();    //класс для кодирования QR в растровом файле
            Bitmap qrImage = qrWrite.Write(qrMatrix);   //создание изображения
            pictureBox1.Image = qrImage;

        }

        internal sealed class Base32
        {
            /// <summary>
            /// Size of the regular byte in bits
            /// </summary>
            private const int InByteSize = 8;

            /// <summary>
            /// Size of converted byte in bits
            /// </summary>
            private const int OutByteSize = 5;

            /// <summary>
            /// Alphabet
            /// </summary>
            private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

            /// <summary>
            /// Convert byte array to Base32 format
            /// </summary>
            /// <param name="bytes">An array of bytes to convert to Base32 format</param>
            /// <returns>Returns a string representing byte array</returns>
            internal static string ToBase32String(byte[] bytes)
            {
                // Check if byte array is null
                if (bytes == null)
                {
                    return null;
                }
                // Check if empty
                else if (bytes.Length == 0)
                {
                    return string.Empty;
                }

                // Prepare container for the final value
                StringBuilder builder = new StringBuilder(bytes.Length * InByteSize / OutByteSize);

                // Position in the input buffer
                int bytesPosition = 0;

                // Offset inside a single byte that <bytesPosition> points to (from left to right)
                // 0 - highest bit, 7 - lowest bit
                int bytesSubPosition = 0;

                // Byte to look up in the dictionary
                byte outputBase32Byte = 0;

                // The number of bits filled in the current output byte
                int outputBase32BytePosition = 0;

                // Iterate through input buffer until we reach past the end of it
                while (bytesPosition < bytes.Length)
                {
                    // Calculate the number of bits we can extract out of current input byte to fill missing bits in the output byte
                    int bitsAvailableInByte = Math.Min(InByteSize - bytesSubPosition, OutByteSize - outputBase32BytePosition);

                    // Make space in the output byte
                    outputBase32Byte <<= bitsAvailableInByte;

                    // Extract the part of the input byte and move it to the output byte
                    outputBase32Byte |= (byte)(bytes[bytesPosition] >> (InByteSize - (bytesSubPosition + bitsAvailableInByte)));

                    // Update current sub-byte position
                    bytesSubPosition += bitsAvailableInByte;

                    // Check overflow
                    if (bytesSubPosition >= InByteSize)
                    {
                        // Move to the next byte
                        bytesPosition++;
                        bytesSubPosition = 0;
                    }

                    // Update current base32 byte completion
                    outputBase32BytePosition += bitsAvailableInByte;

                    // Check overflow or end of input array
                    if (outputBase32BytePosition >= OutByteSize)
                    {
                        // Drop the overflow bits
                        outputBase32Byte &= 0x1F;  // 0x1F = 00011111 in binary

                        // Add current Base32 byte and convert it to character
                        builder.Append(Base32Alphabet[outputBase32Byte]);

                        // Move to the next byte
                        outputBase32BytePosition = 0;
                    }
                }

                // Check if we have a remainder
                if (outputBase32BytePosition > 0)
                {
                    // Move to the right bits
                    outputBase32Byte <<= (OutByteSize - outputBase32BytePosition);

                    // Drop the overflow bits
                    outputBase32Byte &= 0x1F;  // 0x1F = 00011111 in binary

                    // Add current Base32 byte and convert it to character
                    builder.Append(Base32Alphabet[outputBase32Byte]);
                }

                return builder.ToString();
            }

        }

    }
}
