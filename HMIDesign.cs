using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;

namespace CANoeForm
{
    struct testplan
    {
        public string project;
        public string phase;
        public string ecu;
        public string variant;
        public string supplier;
        public string hwversion;
        public string bootversion;
    };

    struct vbfdetails
    {
        public string vbf_filename;
        public string vbf_version;
        public string sw_part_type;
        public string sw_part_number;
        public string sw_version;
        public string sw_current_part_number;
        public string sw_current_version;
        public string data_format_identifier;
        public string ecu_address;
        public int erasecnt;
        public string[] erase_startaddr;
        public string[] erase_length;
        public int blockcnt;
        public string[] block_startaddr;
        public string[] block_length;
        public string[] block_vbfpoint;
        public string[] block_checksum;
        public string call;
        public string verification_block_root_hash;
        public string verification_block_start;
        public string verification_block_length;
        public string sw_signature_dev;
        public string sw_signature;
        public string file_checksum;

        public string file_path_name;
    };

    struct warninginfo
    {
        public string vbfbasicinfo;
        public string sblrelatedinfo;
        public string signatureinfo;
        public string datablockinfo;
    };

    public class HMIDesign
    {
        public static byte vbfcnt = 0;
        public const int MAXMEMORYSIZE = 268435456;
        public const int HeaderSizeMax = 10000;
        private vbfdetails[] vbfdetails = new vbfdetails[1255];
        private SQLHandle downloadConfig = new SQLHandle();
        /// <summary>
        /// 解析vbf,生成版本回退db
        /// </summary>
        /// <param name="vbfFileDirectory"></param>
        /// <param name="secConstant"></param>
        /// <param name="sqliteDbFile"></param>
        public HMIDesign(string vbfFileDirectory, string secConstant, string sqliteDbFile)
        {
            // 解析
            addVbfFiles(vbfFileDirectory);

            //SBL必须在第一位
            var index = Array.FindIndex(vbfdetails, detail => "SBL".Equals(detail.sw_part_type));
            //如果没有找到满足条件的数组元素
            if (index > -1)
            {
                //把满足条件的数组元素赋值给临时变量
                var temp = vbfdetails[index];
                Array.Copy(vbfdetails, 0, vbfdetails, index, 1);
                vbfdetails[0] = temp;
            }
            try
            {
                // 如果存在ESS，必须在第二位
                index = Array.FindIndex(vbfdetails, detail => "ESS".Equals(detail.sw_part_type));
                //如果没有找到满足条件的数组元素
                if (index > 0)
                {
                    //把满足条件的数组元素赋值给临时变量
                    var temp = vbfdetails[index];
                    Array.Copy(vbfdetails, 1, vbfdetails, index, 1);
                    vbfdetails[1] = temp;
                }
            }
            catch (Exception e)
            {
                CommonConstants.logger.WriteLog(e.Message);
            }

            saveDB("", "", "", "", "", "", "", secConstant, sqliteDbFile);
        }
        private void addVbfFiles(string dataFilePath)
        {

            string[] fNames = Directory.GetFiles(dataFilePath, "*.vbf", SearchOption.TopDirectoryOnly);
            for (int j = 0; j < fNames.Count(); j++)
            {
                int fileindex = vbfcnt;
                vbfFileDecode(fNames[j], out vbfdetails[fileindex]);
                vbfcnt++;
            }
        }

        private void vbfDecode(string fileName)
        {
            bool regexResult = false;
            bool checkResult = false;

            string[] fileHeader = new string[HeaderSizeMax];

            int eraseAddress = 0;
            int eraseLength = 0;

            string[] sw_part_type = new string[10];
            string[] sw_part_number = new string[20];
            string[] sw_version = new string[4];

            int DataBlockAddress = 0;
            int DataBlockLength = 0;
            short DataBlockChecksum = 0;
            string[] DataBlockData = new string[0x100000000];

            int charIndex;
        }
        private void vbfFileDecode(string fileName, out vbfdetails vbfdetails)
        {
            string vbf_filename = "";
            string vbf_version = "";
            string sw_part_type = "";
            string sw_part_number = "";
            string sw_version = "";
            string sw_current_part_number = "";
            string sw_current_version = "";
            string data_format_identifier = "";
            string ecu_address = "";
            string erase = "";
            int erasecnt = 0;
            string[] erase_startaddr = new string[65535];
            string[] erase_length = new string[65535];
            int blockcnt = 0;
            string[] block_startaddr = new string[65535];
            string[] block_vbfpoint = new string[65535];
            string[] block_length = new string[65535];
            string[] block_checksum = new string[65535];
            string call = "";
            string verification_block_root_hash = "";
            string verification_block_start = "";
            string verification_block_length = "";
            string sw_signature_dev = "";
            string sw_signature = "";
            string file_checksum = "";

            string VBFpath = fileName;

            bool eraseMatch = false;

            string RegexStr = @";.*";
            Regex regex = new Regex(RegexStr);

            //add code for infor gather
            FileStream debugfile = new FileStream(VBFpath, FileMode.Open, FileAccess.Read);
            StreamReader read = new StreamReader(debugfile, Encoding.Default);

            //FileStream logfile = new FileStream("F:\\VBFdebug.txt", FileMode.Create);

            //string binfilename = Application.StartupPath + "\\VBFbbinsection";

            //
            string strReading = read.ReadLine();

            int binstart = 0;

            byte[] decode = System.Text.Encoding.Default.GetBytes(strReading);
            if (decode[0] == 'v')
            {
                //MessageBox.Show(strReading);
                if (regex.Replace(strReading, "").Replace(" ", "").StartsWith("vbf_version="))
                {
                    vbf_version = strReading.Replace(" ", "").Replace("vbf_version=", "").TrimEnd(";".ToCharArray());
                }
            }
            binstart = binstart + decode.Length + 1;
            //MessageBox.Show(String.Format("binstart = {0:D}", binstart));

            vbf_filename = fileName.Substring(fileName.LastIndexOf("\\") + 1, fileName.Length - fileName.LastIndexOf("\\") - 1);

            while (true)
            {
                strReading = read.ReadLine();
                //MessageBox.Show(strReading);
                decode = System.Text.Encoding.Default.GetBytes(strReading);

                if (decode.Length > 0)
                {
                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("sw_part_type="))
                    {
                        sw_part_type = regex.Replace(strReading, "").Replace(" ", "").Replace("sw_part_type=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("sw_part_number="))
                    {
                        sw_part_number = regex.Replace(strReading, "").Replace(" ", "").Replace("sw_part_number=", "").Replace("\"", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("sw_version="))
                    {
                        sw_version = regex.Replace(strReading, "").Replace(" ", "").Replace("sw_version=", "").Replace("\"", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("sw_current_part_number="))
                    {
                        sw_current_part_number = regex.Replace(strReading, "").Replace(" ", "").Replace("sw_current_part_number=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("sw_current_version="))
                    {
                        sw_current_version = regex.Replace(strReading, "").Replace(" ", "").Replace("sw_current_version=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("data_format_identifier="))
                    {
                        //data_format_identifier = strRea1ding.Replace(" ", "").Replace("data_format_identifier=", "").TrimEnd0(";".ToCharArray()).Replace("\t", "");
                        data_format_identifier = regex.Replace(strReading, "").Replace(" ", "").Replace("data_format_identifier=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("ecu_address="))
                    {
                        ecu_address = regex.Replace(strReading, "").Replace(" ", "").Replace("ecu_address=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("erase="))
                    {
                        erase = regex.Replace(strReading, "").Replace(" ", "").Replace("erase=", "").Replace("\t", "");

                        //if ((erase.StartsWith("{{")) && (erase.EndsWith("}};")))
                        if ((erase.StartsWith("{{")) && (erase.EndsWith("}}")))
                        {
                            eraseMatch = false;
                            erase = erase.Replace("{", "");
                            erase = erase.Replace("}", "");
                            erase = erase.Replace(";", "");
                            erase_startaddr[0] = erase.Split(",".ToCharArray())[0];
                            erase_length[0] = erase.Split(",".ToCharArray())[1];
                            for (int i = 0, j = 0; i < erase.Split(",".ToCharArray()).Count(); i = i + 2, j = j + 1)
                            {
                                erase_startaddr[j] = erase.Split(",".ToCharArray())[i];
                                erase_length[j] = erase.Split(",".ToCharArray())[i + 1];
                                erasecnt++;
                            }
                            //erasecnt++;
                        }
                        else
                        {
                            eraseMatch = true;
                        }
                    }
                    else if (eraseMatch == true)
                    {
                        erase = erase + regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "");
                        //if ((erase.StartsWith("{{")) && (erase.EndsWith("}};")))
                        if ((erase.StartsWith("{{")) && (erase.EndsWith("}}")))
                        {
                            eraseMatch = false;
                            erase = erase.Replace("{", "");
                            erase = erase.Replace("}", "");
                            erase = erase.Replace(";", "");
                            for (int i = 0, j = 0; i < erase.Split(",".ToCharArray()).Count(); i = i + 2, j = j + 1)
                            {
                                erase_startaddr[j] = erase.Split(",".ToCharArray())[i];
                                erase_length[j] = erase.Split(",".ToCharArray())[i + 1];
                                erasecnt = j + 1;
                            }
                        }
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("call="))
                    {
                        call = regex.Replace(strReading, "").Replace(" ", "").Replace("call=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("verification_block_root_hash="))
                    {
                        verification_block_root_hash = regex.Replace(strReading, "").Replace(" ", "").Replace("verification_block_root_hash=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("verification_block_start="))
                    {
                        verification_block_start = regex.Replace(strReading, "").Replace(" ", "").Replace("verification_block_start=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("verification_block_length="))
                    {
                        verification_block_length = regex.Replace(strReading, "").Replace(" ", "").Replace("verification_block_length=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("sw_signature_dev="))
                    {
                        sw_signature_dev = regex.Replace(strReading, "").Replace(" ", "").Replace("sw_signature_dev=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("sw_signature="))
                    {
                        sw_signature = regex.Replace(strReading, "").Replace(" ", "").Replace("sw_signature=", "").Replace("\t", "");
                    }

                    if (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("file_checksum="))
                    {
                        file_checksum = regex.Replace(strReading, "").Replace(" ", "").Replace("file_checksum=", "").Replace("\t", "");
                    }

                    //binstart = binstart + decode.Length;
                    //MessageBox.Show(String.Format("binstart = {0:D}", binstart));

                    //if (decode[0] == '}')
                    if ((eraseMatch == false) && (regex.Replace(strReading, "").Replace(" ", "").Replace("\t", "").StartsWith("}")) && (!(strReading.Replace(" ", "").Replace("\t", "").EndsWith(";"))))
                    {
                        binstart = binstart + 1;
                        break;
                    }
                    else
                    {
                        binstart = binstart + decode.Length + 1;
                        //MessageBox.Show(String.Format("binstart = {0:D}", binstart));
                    }
                }
                else
                {
                    binstart = binstart + 1;
                }
            }

            //MessageBox.Show(String.Format("binstart = {0:D}", binstart));

            debugfile.Close();
            read.Close();

            //

            FileStream BINfile = new FileStream(VBFpath, FileMode.Open, FileAccess.Read);

            //byte[] data = new byte[2147480000];

            byte[] data = new byte[MAXMEMORYSIZE];

            //byte[] DATA = new byte[2147483648];

            long curpoint = 0;

            int lengthdebug = BINfile.Read(data, 0, 1);//非¤?bin文?件t剔¬T除y

            curpoint++;

            int newlinef = 1, commentflag = 0, bracecnt = 0;

            while (true)
            {
                BINfile.Position = curpoint;
                lengthdebug = BINfile.Read(data, 0, 2);
                curpoint++;
                if (lengthdebug > 0)
                {
                    if ((data[0] == 0x0D) || (data[0] == 0x0A))
                    {
                        commentflag = 0;
                        continue;
                    }

                    if (commentflag == 0)
                    {
                        if ((data[0] == '/') && (data[1] == '/'))
                        {
                            curpoint++;
                            commentflag = 1;
                            continue;
                        }

                        if (data[0] == '{')
                        {
                            bracecnt++;
                        }
                        else if (data[0] == '}')
                        {
                            bracecnt--;
                            if (bracecnt == 0)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        continue;
                    }

                    //if (newlinef == 1)
                    //{
                    //    if(data[0] == '}')
                    //    {
                    //        break;
                    //    }
                    //    else if((data[0] != ' ')&&(data[0] != '\t'))
                    //    {
                    //        newlinef = 0;
                    //    }
                    //}
                    //else
                    //{
                    //    if (data[0] == 0x0A)
                    //    {
                    //        newlinef = 1;
                    //        commentflag = 0;
                    //    }
                    //    else
                    //    {
                    //        newlinef = 0;
                    //    }
                    //}
                }
            }

            //
            BINfile.Position = curpoint;
            int FSinfor = BINfile.Read(data, 0, 4);

            blockcnt = 0;
            if (FSinfor == 4)
            {
                byte[] FSadress = new byte[4];
                curpoint = curpoint + 4;
                FSadress[0] = data[0];
                FSadress[1] = data[1];
                FSadress[2] = data[2];
                FSadress[3] = data[3];
                block_startaddr[0] = "0x" + FSadress[0].ToString("X2") + FSadress[1].ToString("X2") + FSadress[2].ToString("X2") + FSadress[3].ToString("X2");
                //MessageBox.Show(String.Format("FSadress = {0:X2} {1:X2} {2:X2} {3:X2}", FSadress[0], FSadress[1], FSadress[2], FSadress[3]));
                FSinfor = BINfile.Read(data, 0, 4);
                byte[] FSlength = new byte[4];
                curpoint = curpoint + 4;
                block_vbfpoint[0] = curpoint.ToString("D");

                FSlength[0] = data[0];
                FSlength[1] = data[1];
                FSlength[2] = data[2];
                FSlength[3] = data[3];
                block_length[0] = "0x" + FSlength[0].ToString("X2") + FSlength[1].ToString("X2") + FSlength[2].ToString("X2") + FSlength[3].ToString("X2");
                //MessageBox.Show(String.Format("FSlength = {0:X2} {1:X2} {2:X2} {3:X2}", FSlength[0], FSlength[1], FSlength[2], FSlength[3]));
                long FSlgth = 0;
                long HFSlgth = 0;

                FSlgth = FSlength[0] * 16777216 + FSlength[1] * 65536 + FSlength[2] * 256 + FSlength[3];

                int SN = 1;
                blockcnt = 1;
                //binfilename = binfilename + "1.txt";
                //FileStream logfile = new FileStream(binfilename, FileMode.Create);

                HFSlgth = FSlgth;
                while (HFSlgth > 0)
                {
                    if (HFSlgth > MAXMEMORYSIZE)
                    {
                        FSlgth = MAXMEMORYSIZE;
                    }
                    else
                    {
                        FSlgth = HFSlgth;
                    }

                    lengthdebug = BINfile.Read(data, 0, (int)FSlgth);
                    curpoint = curpoint + FSlgth;

                    //for (int i = 0; i < lengthdebug; i++)
                    //{
                    //    if ((byte)(data[i] / 16) < 10)
                    //    {
                    //        DATA[i * 2] = (byte)(data[i] / 16 + 48);
                    //    }
                    //    else
                    //    {
                    //        DATA[i * 2] = (byte)(data[i] / 16 - 9 + 64);
                    //    }

                    //    if ((byte)(data[i] % 16) < 10)
                    //    {
                    //        DATA[i * 2 + 1] = (byte)(data[i] % 16 + 48);
                    //    }
                    //    else
                    //    {
                    //        DATA[i * 2 + 1] = (byte)(data[i] % 16 - 9 + 64);
                    //    }

                    //}

                    //logfile.Write(data, 0, lengthdebug);
                    ////logfile.Write(DATA, 0, 2 * lengthdebug);
                    HFSlgth = HFSlgth - MAXMEMORYSIZE;
                }
                //logfile.Flush();
                //logfile.Close();

                while (true)
                {
                    lengthdebug = BINfile.Read(data, 0, 2);
                    curpoint = curpoint + 2;

                    if (lengthdebug == 0)
                        break;
                    else
                    {
                        block_checksum[SN - 1] = "0x" + (data[0] * 256 + data[1]).ToString("X2");

                        FSinfor = BINfile.Read(data, 0, 4);
                        curpoint = curpoint + 4;

                        if (FSinfor < 4)
                            break;

                        FSadress[0] = data[0];
                        FSadress[1] = data[1];
                        FSadress[2] = data[2];
                        FSadress[3] = data[3];
                        block_startaddr[SN] = "0x" + FSadress[0].ToString("X2") + FSadress[1].ToString("X2") + FSadress[2].ToString("X2") + FSadress[3].ToString("X2");
                        //MessageBox.Show(String.Format("FSadress = {0:X2} {1:X2} {2:X2} {3:X2}", FSadress[0], FSadress[1], FSadress[2], FSadress[3]));
                        FSinfor = BINfile.Read(data, 0, 4);
                        curpoint = curpoint + 4;
                        block_vbfpoint[SN] = curpoint.ToString("D");

                        FSlength[0] = data[0];
                        FSlength[1] = data[1];
                        FSlength[2] = data[2];
                        FSlength[3] = data[3];
                        block_length[SN] = "0x" + FSlength[0].ToString("X2") + FSlength[1].ToString("X2") + FSlength[2].ToString("X2") + FSlength[3].ToString("X2");
                        //MessageBox.Show(String.Format("FSlength = {0:X2} {1:X2} {2:X2} {3:X2}", FSlength[0], FSlength[1], FSlength[2], FSlength[3]));
                        FSlgth = 0;
                        FSlgth = (FSlength[0] * 16777216 + FSlength[1] * 65536 + FSlength[2] * 256 + FSlength[3]) & 0x00000000FFFFFFFF;

                        SN++;
                        blockcnt = SN;
                        //binfilename = Application.StartupPath + "\\VBFbbinsection";
                        //binfilename = binfilename + String.Format("{0:D}", SN) + ".txt";
                        //logfile = new FileStream(binfilename, FileMode.Create);

                        HFSlgth = FSlgth;
                        while (HFSlgth > 0)
                        {
                            if (HFSlgth > MAXMEMORYSIZE)
                            {
                                FSlgth = MAXMEMORYSIZE;
                            }
                            else
                            {
                                FSlgth = HFSlgth;
                            }

                            lengthdebug = BINfile.Read(data, 0, (int)FSlgth);
                            curpoint = curpoint + FSlgth;

                            //for (int i = 0; i < lengthdebug; i++)
                            //{
                            //    if ((byte)(data[i] / 16) < 10)
                            //    {
                            //        DATA[i * 2] = (byte)(data[i] / 16 + 48);
                            //    }
                            //    else
                            //    {
                            //        DATA[i * 2] = (byte)(data[i] / 16 - 9 + 64);
                            //    }

                            //    if ((byte)(data[i] % 16) < 10)
                            //    {
                            //        DATA[i * 2 + 1] = (byte)(data[i] % 16 + 48);
                            //    }
                            //    else
                            //    {
                            //        DATA[i * 2 + 1] = (byte)(data[i] % 16 - 9 + 64);
                            //    }

                            //}

                            //logfile.Write(data, 0, lengthdebug);
                            ////logfile.Write(DATA, 0, 2 * lengthdebug);
                            HFSlgth = HFSlgth - MAXMEMORYSIZE;
                        }
                        //logfile.Flush();
                        //logfile.Close();
                    }
                }
            }
            BINfile.Close();

            //vbfdetails.vbfcnt++;
            vbfdetails.vbf_filename = vbf_filename;
            vbfdetails.vbf_version = vbf_version;
            vbfdetails.sw_part_type = sw_part_type;
            vbfdetails.sw_part_number = sw_part_number;
            vbfdetails.sw_version = sw_version;
            vbfdetails.sw_current_part_number = sw_current_part_number;
            vbfdetails.sw_current_version = sw_current_version;
            vbfdetails.data_format_identifier = data_format_identifier;
            vbfdetails.ecu_address = ecu_address;
            vbfdetails.erasecnt = erasecnt;
            vbfdetails.erase_startaddr = erase_startaddr;
            vbfdetails.erase_length = erase_length;
            vbfdetails.blockcnt = blockcnt;
            vbfdetails.block_startaddr = block_startaddr;
            vbfdetails.block_vbfpoint = block_vbfpoint;
            vbfdetails.block_length = block_length;
            vbfdetails.block_checksum = block_checksum;
            vbfdetails.call = call;
            vbfdetails.verification_block_root_hash = verification_block_root_hash;
            vbfdetails.verification_block_start = verification_block_start;
            vbfdetails.verification_block_length = verification_block_length;
            vbfdetails.sw_signature_dev = sw_signature_dev;
            vbfdetails.sw_signature = sw_signature;
            vbfdetails.file_checksum = file_checksum;
            vbfdetails.file_path_name = fileName;
            //MessageBox.Show("vbf_version = " + vbf_version + "\n" +
            //    "sw_part_type = " + sw_part_type + "\n" +
            //    "sw_part_number = " + sw_part_number + "\n" +
            //    "sw_version = " + sw_version + "\n" +
            //    "sw_current_part_number = " + sw_current_part_number + "\n" +
            //    "sw_current_version = " + sw_current_version + "\n" +
            //    "data_format_identifier = " + data_format_identifier + "\n" +
            //    "ecu_address = " + ecu_address + "\n" +
            //    "erase = " + erase + "\n" +
            //    "erasecnt = " + erasecnt + "\n" +
            //    "erase_startaddr = " + erase_startaddr[0] + "," + erase_startaddr[1] + "," + erase_startaddr[2] + "," + erase_startaddr[3] + "," + erase_startaddr[4] + "\n" +
            //    "erase_length = " + erase_length[0] + "," + erase_length[1] + "," + erase_length[2] + "," + erase_length[3] + "," + erase_length[4] + "\n" +
            //    "blockcnt = " + blockcnt + "\n" +
            //    "block_startaddr = " + block_startaddr[0] + "," + block_startaddr[1] + "," + block_startaddr[2] + "," + block_startaddr[3] + "," + block_startaddr[4] + "\n" +
            //    "block_length = " + block_length[0] + "," + block_length[1] + "," + block_length[2] + "," + block_length[3] + "," + block_length[4] + "\n" +
            //    "block_vbfpoint = " + block_vbfpoint[0] + "," + block_vbfpoint[1] + "," + block_vbfpoint[2] + "," + block_vbfpoint[3] + "," + block_vbfpoint[4] + "\n" +
            //    "block_checksum = " + block_checksum[0] + "," + block_checksum[1] + "," + block_checksum[2] + "," + block_checksum[3] + "," + block_checksum[4] + "\n" +
            //    "call = " + call + "\n" +
            //    "verification_block_root_hash = " + verification_block_root_hash + "\n" +
            //    "verification_block_root_hash = " + verification_block_root_hash + "\n" +
            //    "verification_block_start = " + verification_block_start + "\n" +
            //    "verification_block_length = " + verification_block_length + "\n" +
            //    "sw_signature_dev = " + sw_signature_dev + "\n" +
            //    "sw_signature = " + sw_signature + "\n" +
            //    "file_checksum = " + file_checksum + "\n"
            //    ,"vbfDecodeResult");
        }


        private void saveDB(string project, string phase, string ecu, string variant, string supplier, string hwversion, string bootversion, string secConstant, string sqliteDbFile)
        {
            secConstant = secConstant.Replace("0x", "");
            secConstant = secConstant.Replace("0X", "");
            secConstant = "0x" + secConstant;

            string fieldvalue = "";
            downloadConfig.CreatDBConnect(sqliteDbFile);
            downloadConfig.CreatTable("TestPlan", "ID VARCHAR,Model VARCHAR,Phase VARCHAR,ECUName VARCHAR,ECUVariant VARCHAR,Supplier VARCHAR,HWversion VARCHAR,BootSWID VARCHAR,SignEnable VARCHAR,PowerOnOnly VARCHAR,SecConstant VARCHAR");

            downloadConfig.DeleteItem("TestPlan", "1");
            downloadConfig.CreatTable("FileInfo", "ID VARCHAR,BootSWID VARCHAR,vbf_filename VARCHAR,vbf_version VARCHAR,sw_part_type VARCHAR,file_index VARCHAR,sw_part_number VARCHAR,sw_version VARCHAR,sw_current_part_number VARCHAR,sw_current_version VARCHAR,data_format_identifier VARCHAR,ecu_address VARCHAR,erasecnt VARCHAR,blockcnt VARCHAR,call VARCHAR,verification_block_root_hash VARCHAR,verification_block_start VARCHAR,verification_block_length VARCHAR,sw_signature_dev VARCHAR,sw_signature VARCHAR,file_checksum VARCHAR");
            downloadConfig.DeleteItem("FileInfo", "1");
            downloadConfig.CreatTable("EraseInfo", "FileName VARCHR,EraseIndex VARCHR,StartAddress VARCHR,Length VARCHR");
            downloadConfig.DeleteItem("EraseInfo", "1");
            downloadConfig.CreatTable("DataInfo", "FileName VARCHR,DataBlockIndex VARCHR,StartAddr VARCHR,DataBlockSize VARCHR,CheckSum VARCHR,DataBlockVBFPoint VARCHR");
            downloadConfig.DeleteItem("DataInfo", "1");

            fieldvalue = "'" + "1" + "','" + project + "','" + phase + "','" + ecu + "','" + variant + "','" + supplier + "','" + hwversion + "','" + bootversion + "','" + Convert.ToUInt16(false) + "','" + Convert.ToUInt16(false) + "','" + secConstant + "'";
            downloadConfig.InsertItem("TestPlan", "ID,Model,Phase,ECUName,ECUVariant,Supplier,HWversion,BootSWID,SignEnable,PowerOnOnly,SecConstant", fieldvalue);

            uint res = 0;
            string sqlclause = "";
            warninginfo checkinfo;

            checkinfo.vbfbasicinfo = "";
            checkinfo.sblrelatedinfo = "";
            checkinfo.signatureinfo = "";
            checkinfo.datablockinfo = "";

            bool vbf26VBTvalidity = false;

            //for (int j = 0; Filelist.Items.Count; j++)
            //{
            //    int fileindex = vbfcnt;

            //    vbfFileDecode(fNames[j], out vbfdetails[fileindex]);
            //}

            for (int i = 0; i < vbfdetails.Length; i++)
            {
                if (string.IsNullOrEmpty(vbfdetails[i].sw_part_type))
                {
                    continue;
                }
                vbf26VBTvalidity = false;

                sqlclause = "Select Count(*) from FileInfo WHERE sw_part_type = '" + vbfdetails[i].sw_part_type + "'";
                downloadConfig.CountItem(sqlclause, ref res);

                fieldvalue = "'" + (i + 1).ToString()
                             + "','" + bootversion
                             + "','" + vbfdetails[i].vbf_filename
                             + "','" + vbfdetails[i].vbf_version
                             + "','" + vbfdetails[i].sw_part_type
                             + "','" + (res + 1).ToString()
                             + "','" + vbfdetails[i].sw_part_number
                             + "','" + vbfdetails[i].sw_version
                             + "','" + vbfdetails[i].sw_current_part_number
                             + "','" + vbfdetails[i].sw_current_version
                             + "','" + vbfdetails[i].data_format_identifier
                             + "','" + vbfdetails[i].ecu_address
                             + "','" + vbfdetails[i].erasecnt
                             + "','" + vbfdetails[i].blockcnt
                             + "','" + vbfdetails[i].call
                             + "','" + vbfdetails[i].verification_block_root_hash
                             + "','" + vbfdetails[i].verification_block_start
                             + "','" + vbfdetails[i].verification_block_length
                             + "','" + vbfdetails[i].sw_signature_dev
                             + "','" + vbfdetails[i].sw_signature
                             + "','" + vbfdetails[i].file_checksum + "'";
                downloadConfig.InsertItem("FileInfo", "ID,BootSWID,vbf_filename,vbf_version,sw_part_type,file_index,sw_part_number,sw_version,sw_current_part_number,sw_current_version,data_format_identifier,ecu_address,erasecnt,blockcnt,call,verification_block_root_hash,verification_block_start,verification_block_length,sw_signature_dev,sw_signature,file_checksum", fieldvalue);

                vbfFileDecode(vbfdetails[i].file_path_name, out vbfdetails[i]);

                //byte k;
                //for (k = 0; k < Filelist.Items.Count; k++)
                //{
                //    if (vbfdetails[i].vbf_filename == vbfdetails[k].vbf_filename)
                //    {
                //        break;
                //    }
                //}

                //for (int j = 0; j < Convert.ToUInt16(vbfdetails[i].erasecnt); j++)
                for (int j = 0; j < vbfdetails[i].erasecnt; j++)
                {
                    fieldvalue = "'" + vbfdetails[i].vbf_filename
                                 + "','" + (j + 1).ToString()
                                 + "','" + vbfdetails[i].erase_startaddr[j]
                                 + "','" + vbfdetails[i].erase_length[j] + "'";
                    downloadConfig.InsertItem("EraseInfo", "FileName,EraseIndex,StartAddress,Length", fieldvalue);

                }

                bool datablockwarnflag = false;
                //for (int j = 0; j < Convert.ToUInt16(vbfdetails[i].blockcnt); j++)
                for (int j = 0; j < vbfdetails[i].blockcnt; j++)
                {
                    fieldvalue = "'" + vbfdetails[i].vbf_filename
                                 + "','" + (j + 1).ToString()
                                 + "','" + vbfdetails[i].block_startaddr[j]
                                 + "','" + vbfdetails[i].block_length[j]
                                 + "','" + vbfdetails[i].block_checksum[j]
                                 + "','" + vbfdetails[i].block_vbfpoint[j] + "'";

                    //lzss lzss = new lzss();
                    //FileStream BINfile = new FileStream("C:\\Users\\WCH\\Desktop\\8890093431B\\8890093431B.vbf", FileMode.Open, FileAccess.Read);

                    //byte[] vbfdata = new byte[1000000];
                    //byte[] uncompout = new byte[1000000];

                    //int vbfoffset = Convert.ToInt32(vbfdetails[k].block_vbfpoint[j], 10);
                    //int vbfcount = Convert.ToInt32(vbfdetails[k].block_length[j], 16);

                    //int lengthdebug = BINfile.Read(vbfdata, 0, vbfoffset);
                    //lengthdebug = BINfile.Read(vbfdata, 0, vbfcount);

                    //unsafe
                    //{
                    //    fixed (byte* datain = vbfdata, dataout = uncompout)
                    //    {
                    //        lzss.lzssUnCompress(datain, (uint)lengthdebug, dataout);
                    //    }
                    //}


                    if ((vbfdetails[i].vbf_version == "2.6") && (vbfdetails[i].verification_block_start != "") && (vbfdetails[i].verification_block_length != ""))
                    {
                        //if ((Convert.ToUInt32(vbfdetails[k].block_startaddr[j], 16) == (Convert.ToUInt32(vbfdetails[k].verification_block_start, 16))) &&
                        //   (Convert.ToUInt32(vbfdetails[k].block_length[j], 16) == (Convert.ToUInt32(vbfdetails[k].verification_block_length, 16))))
                        if ((Convert.ToUInt32(vbfdetails[i].block_startaddr[j], 16)) == (Convert.ToUInt32(vbfdetails[i].verification_block_start, 16)))
                        {

                            vbf26VBTvalidity = true;
                        }
                    }

                    if ((vbfdetails[i].block_checksum[j] == "") || (vbfdetails[i].block_checksum[j] == null))
                    {
                        checkinfo.datablockinfo = checkinfo.datablockinfo + "In" + vbfdetails[i].vbf_filename + "(block" + (j + 1) + ")" + ": real datablock length is less than block_length parameter.\n";
                    }


                    if (j < 15)
                    {
                        for (int b = 0; b < j; b++)
                        {
                            if ((Convert.ToUInt32(vbfdetails[i].block_startaddr[j], 16) >= Convert.ToUInt32(vbfdetails[i].block_startaddr[b], 16)) &&
                                ((Convert.ToUInt32(vbfdetails[i].block_startaddr[j], 16) + Convert.ToUInt32(vbfdetails[i].block_length[j], 16)) <= (Convert.ToUInt32(vbfdetails[i].block_startaddr[b], 16) + Convert.ToUInt32(vbfdetails[i].block_length[b], 16))))
                            {
                                checkinfo.datablockinfo = checkinfo.datablockinfo + "In" + vbfdetails[i].vbf_filename + ": datablock" + (j + 1) + " has overlapping address mapping with datablock" + (b + 1) + ".\n";
                                datablockwarnflag = true;
                            }
                        }
                    }
                    else if ((datablockwarnflag == true) && (j >= (vbfdetails[i].blockcnt - 1)))
                    {
                        checkinfo.datablockinfo = checkinfo.datablockinfo + "In" + vbfdetails[i].vbf_filename + ": datablock with index which is more than 15 will not be checked to save the processing time, please check them manually!";
                    }

                    downloadConfig.InsertItem("DataInfo", "FileName,DataBlockIndex,StartAddr,DataBlockSize,CheckSum,DataBlockVBFPoint", fieldvalue);
                }

                if ((vbfdetails[i].vbf_version != "2.5") && (vbfdetails[i].vbf_version != "2.6"))
                {
                    checkinfo.vbfbasicinfo = checkinfo.vbfbasicinfo + "In" + vbfdetails[i].vbf_filename + ": vbf version is not correct(2.5/2.6).\n";
                }

                if ((vbfdetails[i].sw_part_type == "SBL") && (vbfdetails[i].call == ""))
                {
                    checkinfo.sblrelatedinfo = checkinfo.sblrelatedinfo + "In" + vbfdetails[i].vbf_filename + ": SBL files has blank call information.\n";
                }

                if ((vbfdetails[i].sw_part_type == "SBL") && (vbfdetails[i].erasecnt.ToString() != "0"))
                {
                    checkinfo.sblrelatedinfo = checkinfo.sblrelatedinfo + "In" + vbfdetails[i].vbf_filename + ": SBL files has invalid eraseblock information.\n";
                }

                if ((vbfdetails[i].erasecnt.ToString() == "0") && (vbfdetails[i].blockcnt.ToString() == "0"))
                {
                    checkinfo.vbfbasicinfo = checkinfo.vbfbasicinfo + "In" + vbfdetails[i].vbf_filename + ": Files has neither erase blocks nor data blocks.\n";
                }

                if ((vbfdetails[i].vbf_version == "2.6") && (vbf26VBTvalidity == false))
                {
                    checkinfo.vbfbasicinfo = checkinfo.signatureinfo + "In" + vbfdetails[i].vbf_filename + ": vbf2.6 file has no verification block table .\n";
                }
                vbf26VBTvalidity = false;

                //if ((vbfdetails[i].vbf_version == "2.6") && (vbfdetails[i].blockcnt != "0") && ((vbfdetails[i].verification_block_root_hash == "") ||
                //    (vbfdetails[i].verification_block_start == "") || (vbfdetails[i].verification_block_length == "") || ((vbfdetails[i].sw_signature_dev != "")&&( vbfdetails[i].sw_signature != "")) ||
                //    (vbfdetails[i].verification_block_length == vbfdetails[i].sw_signature_dev) || (vbfdetails[i].verification_block_length == vbfdetails[i].sw_signature)))
                if ((vbfdetails[i].vbf_version == "2.6") && (vbfdetails[i].blockcnt.ToString() != "0") && ((vbfdetails[i].verification_block_root_hash == "") ||
                    (vbfdetails[i].verification_block_start == "") || (vbfdetails[i].verification_block_length == "") || ((false == false) && (vbfdetails[i].sw_signature_dev == "")) || ((false == true) && (vbfdetails[i].sw_signature == "")) ||
                    (vbfdetails[i].verification_block_length == vbfdetails[i].sw_signature_dev) || (vbfdetails[i].verification_block_length == vbfdetails[i].sw_signature)))
                {
                    checkinfo.signatureinfo = checkinfo.signatureinfo + "In" + vbfdetails[i].vbf_filename + ": vbf2.6 file has incomplete signature infomation .\n";
                }

                if (i >= 1)
                {
                    if (vbfdetails[i].vbf_version != vbfdetails[i - 1].vbf_version)
                    {
                        checkinfo.vbfbasicinfo = checkinfo.vbfbasicinfo + "In" + vbfdetails[i].vbf_filename + ": vbfversion is in inconsistent with previous one.\n";
                    }

                    if (vbfdetails[i].ecu_address != vbfdetails[i - 1].ecu_address)
                    {
                        checkinfo.vbfbasicinfo = checkinfo.vbfbasicinfo + "In" + vbfdetails[i].vbf_filename + ": ecu_address is in inconsistent with previous one.\n";
                    }

                    if ((vbfdetails[i].vbf_version == "2.6") && (vbfdetails[i].blockcnt.ToString() != "0") && ((vbfdetails[i].sw_signature_dev == "") ^ (vbfdetails[i - 1].sw_signature_dev == "")))
                    {
                        checkinfo.signatureinfo = checkinfo.signatureinfo + "In" + vbfdetails[i].vbf_filename + ": Vbf2.6 file has inconsistent signature infomation with previous one.\n";
                    }
                }

            }

            if ((checkinfo.vbfbasicinfo != "") || (checkinfo.sblrelatedinfo != "") || (checkinfo.signatureinfo != "") || (checkinfo.datablockinfo != ""))
            {
                string finalinfo = "";
                if (checkinfo.vbfbasicinfo != "")
                {
                    finalinfo = finalinfo + "vbfbasicinfo:\n" + checkinfo.vbfbasicinfo + "\n\n";
                }

                if (checkinfo.sblrelatedinfo != "")
                {
                    finalinfo = finalinfo + "sblrelatedinfo:\n" + checkinfo.sblrelatedinfo + "\n\n";
                }

                if (checkinfo.signatureinfo != "")
                {
                    finalinfo = finalinfo + "signatureinfo:\n" + checkinfo.signatureinfo + "\n\n";
                }

                if (checkinfo.datablockinfo != "")
                {
                    finalinfo = finalinfo + "datablockinfo:\n" + checkinfo.datablockinfo;
                }

                //MessageBox.Show(finalinfo, "Inconsistent check result");
            }

            //saveFormSetting();
            downloadConfig.ColseDB();
            //this.Close();
        }
    }

    public class SQLHandle
    {
        public List<string> ParsCfg = new List<string>();
        private SQLiteConnection conn = null;
        private SQLiteTransaction trans = null;
        private SQLiteCommand sqlcmd = null;

        Hashtable cmdTable = new Hashtable();
        Hashtable parTable = new Hashtable();

        //创建一个空的数据库
        void CreateNewDatabase()
        {
            SQLiteConnection.CreateFile("MyDatabase.sqlite");
        }

        public bool CreatDBConnect(string Path)
        {
            string dbPath = "Data Source =" + Path;
            conn = new SQLiteConnection(dbPath);//创建数据库实例，指定文件位置   
            conn.Open();//打开数据库，若文件不存在会自动创建
            //trans = conn.BeginTransaction();
            return true;
        }

        public bool CreatTable(string TableName, string Fields)
        {
            try
            {
                string sql = "CREATE TABLE IF NOT EXISTS ";//建表语句   

                sql = sql + TableName + " (" + Fields + ");";
                SQLiteCommand sqlitecmd = new SQLiteCommand(sql, conn);
                sqlitecmd.ExecuteNonQuery();//如果表不存在，创建数据表
                return true;
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show("Create Table Failed! Error info:", ex.Message);
                return false;
            }
        }

        public bool InsertItem(string TableName, string Feilds, string Values)
        {
            try
            {
                string sql = "INSERT INTO " + TableName + " (" + Feilds + ") " + "Values" + " (" + Values + ")";//插入数据语句
                SQLiteCommand sqlitecmd = new SQLiteCommand(sql, conn);
                sqlitecmd.ExecuteNonQuery();
                return true;
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show("Insert Item Failed!", ex.Message);
                return false;
            }
        }

        public bool SearchItem0(string TableName, string Feilds, string Conditions, ref DataTable result)
        {
            try
            {
                string sql = "SELECT " + Feilds + " FROM " + TableName + " WHERE " + Conditions;//搜索数据语句
                SQLiteCommand sqlitecmd = new SQLiteCommand(sql, conn);
                SQLiteDataReader reader = sqlitecmd.ExecuteReader();

                //SQLiteCommand sqlitecmd = conn.CreateCommand();
                //sqlitecmd.CommandText = sql;
                //SQLiteDataReader reader = sqlitecmd.ExecuteReader();

                int j = 0;

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    result.Columns.Add();
                }

                while (reader.Read())
                {
                    result.Rows.Add();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        //Debug.Write(reader[i]);
                        //Debug.Write("   ");
                        result.Rows[j][i] = reader[i];
                    }
                    j++;
                    //Debug.WriteLine("","");
                }
                return true;
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show("Search Feild Values Failed!", ex.Message);
                return false;
            }
        }

        public bool SearchItem(string SqlClause, ref DataTable result)
        {
            try
            {
                string sql = SqlClause;//搜索数据语句
                SQLiteCommand sqlitecmd = new SQLiteCommand(sql, conn);
                SQLiteDataReader reader = sqlitecmd.ExecuteReader();

                //SQLiteCommand sqlitecmd = conn.CreateCommand();
                //sqlitecmd.CommandText = sql;
                //SQLiteDataReader reader = sqlitecmd.ExecuteReader();

                int j = 0;

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    result.Columns.Add();
                }

                while (reader.Read())
                {
                    result.Rows.Add();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        //Debug.Write(reader[i]);
                        //Debug.Write("   ");
                        result.Rows[j][i] = reader[i];
                    }
                    j++;
                    //Debug.WriteLine("","");
                }
                return true;
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show("Search Feild Values Failed!", ex.Message);
                return false;
            }
        }

        public bool CountItem(string SqlClause, ref uint result)
        {
            try
            {
                string sql = SqlClause;//搜索数据语句
                SQLiteCommand sqlitecmd = new SQLiteCommand(sql, conn);
                SQLiteDataReader reader = sqlitecmd.ExecuteReader();

                if (reader.Read())
                {
                    result = Convert.ToUInt32(reader[0].ToString());
                }
                else
                {
                    result = 0;
                }
                return true;
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show("Count Feild Values Failed!", ex.Message);
                return false;
            }
        }

        public bool UpdateItem(string TableName, string FeildsUpdateValue, string Conditons)
        {
            try
            {
                string sql = "UPDATE " + TableName + " SET " + FeildsUpdateValue + " WHERE " + Conditons;//搜索数据语句
                SQLiteCommand sqlitecmd = new SQLiteCommand(sql, conn);
                sqlitecmd.ExecuteNonQuery();
                return true;
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show("Update Items Value Failed!", ex.Message);
                return false;
            }
        }

        public bool AddColumn(string TableName, string FeildsAndTypes)
        {
            try
            {
                string sql = "ALTER TABLE " + TableName + " ADD COLUMN " + FeildsAndTypes;//搜索数据语句
                SQLiteCommand sqlitecmd = new SQLiteCommand(sql, conn);
                sqlitecmd.ExecuteNonQuery();
                return true;
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show("Add Column Failed!", ex.Message);
                return false;
            }
        }

        public bool DeleteItem(string TableName, string Conditions)
        {
            try
            {
                string sql = "DELETE " + " FROM " + TableName + " WHERE " + Conditions;//删除数据语句
                SQLiteCommand sqlitecmd = new SQLiteCommand(sql, conn);
                sqlitecmd.ExecuteNonQuery();
                return true;
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show("Delete Items Failed!", ex.Message);
                return false;
            }
        }

        public void ColseDB()
        {
            //trans.Commit();
            conn.Close();
        }

    }
}