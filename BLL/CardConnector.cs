﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRegistration.BLL
{
    class CardConnector
    {
        int retCode;
        int hCard;
        int hContext;
        int Protocol;
        public bool connActive = false;
        string readername = "ACS ACR122 0";      // change depending on reader
        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];
        public int SendLen, RecvLen, nBytesRet, reqType, Aprotocol, dwProtocol, cbPciLength;

        public Card.SCARD_READERSTATE RdrState;
        public Card.SCARD_IO_REQUEST pioSendRequest;
        public CardConnector()
        {
            SelectDevice();
            establishContext();
        }
        #region card reading
        public string verifyCard(String Block)
        {
            string value = "";
            if (connectCard())
            {
                value = readBlock(Block);
            }

            value = value.Split(new char[] { '\0' }, 2, StringSplitOptions.None)[0].ToString();
            return value;
        }

        public string readBlock(String Block)
        {
            string tmpStr = "";
            int indx;

            if (authenticateBlock(Block))
            {
                ClearBuffers();
                SendBuff[0] = 0xFF; // CLA 
                SendBuff[1] = 0xB0;// INS
                SendBuff[2] = 0x00;// P1
                SendBuff[3] = (byte)int.Parse(Block);// P2 : Block No.
                SendBuff[4] = (byte)int.Parse("16");// Le

                SendLen = 5;
                RecvLen = SendBuff[4] + 2;

                retCode = SendAPDUandDisplay(2);

                if (retCode == -200)
                {
                    return "outofrangeexception";
                }

                if (retCode == -202)
                {
                    return "BytesNotAcceptable";
                }

                if (retCode != Card.SCARD_S_SUCCESS)
                {
                    return "FailRead";
                }

                // Display data in text format
                for (indx = 0; indx <= RecvLen - 1; indx++)
                {
                    tmpStr = tmpStr + Convert.ToChar(RecvBuff[indx]);
                }

                return (tmpStr);
            }
            else
            {
                return "FailAuthentication";
            }
        }
        //disconnect card reader connection
        public void Close()
        {
            if (connActive)
            {
                retCode = Card.SCardDisconnect(hCard, Card.SCARD_UNPOWER_CARD);
            }
            //retCode = Card.SCardReleaseContext(hCard);
        }
        //submit data method
        public String submitText(String Text, String Block)
        {

            String tmpStr = Text;
            int indx;
            if (authenticateBlock(Block))
            {
                ClearBuffers();
                SendBuff[0] = 0xFF;                             // CLA
                SendBuff[1] = 0xD6;                             // INS
                SendBuff[2] = 0x00;                             // P1
                SendBuff[3] = (byte)int.Parse(Block);           // P2 : Starting Block No.
                SendBuff[4] = (byte)int.Parse("16");            // P3 : Data length

                for (indx = 0; indx <= (tmpStr).Length - 1; indx++)
                {
                    SendBuff[indx + 5] = (byte)tmpStr[indx];
                }
                SendLen = SendBuff[4] + 5;
                RecvLen = 0x02;

                retCode = SendAPDUandDisplay(2);

                if (retCode != Card.SCARD_S_SUCCESS)
                {
                    return "fail write";

                }
                else
                {
                    return "write success";
                }
            }
            else
            {
                return "FailAuthentication";
            }
        }

        // block authentication
        private bool authenticateBlock(String block)
        {
            ClearBuffers();
            SendBuff[0] = 0xFF;                         // CLA
            SendBuff[2] = 0x00;                         // P1: same for all source types 
            SendBuff[1] = 0x86;                         // INS: for stored key input
            SendBuff[3] = 0x00;                         // P2 : Memory location;  P2: for stored key input
            SendBuff[4] = 0x05;                         // P3: for stored key input
            SendBuff[5] = 0x01;                         // Byte 1: version number
            SendBuff[6] = 0x00;                         // Byte 2
            SendBuff[7] = (byte)int.Parse(block);       // Byte 3: sectore no. for stored key input
            SendBuff[8] = 0x60;                         // Byte 4 : Key A for stored key input
            SendBuff[9] = (byte)int.Parse("1");         // Byte 5 : Session key for non-volatile memory

            SendLen = 0x0A;
            RecvLen = 0x02;

            retCode = SendAPDUandDisplay(0);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                //MessageBox.Show("FAIL Authentication!");
                return false;
            }

            return true;
        }

        // clear memory buffers
        private void ClearBuffers()
        {
            long indx;

            for (indx = 0; indx <= 262; indx++)
            {
                RecvBuff[indx] = 0;
                SendBuff[indx] = 0;
            }
        }

        // send application protocol data unit : communication unit between a smart card reader and a smart card
        private int SendAPDUandDisplay(int reqType)
        {
            int indx;
            string tmpStr = "";

            pioSendRequest.dwProtocol = Aprotocol;
            pioSendRequest.cbPciLength = 8;

            //Display Apdu In
            for (indx = 0; indx <= SendLen - 1; indx++)
            {
                tmpStr = tmpStr + " " + string.Format("{0:X2}", SendBuff[indx]);
            }

            retCode = Card.SCardTransmit(hCard, ref pioSendRequest, ref SendBuff[0],
                                 SendLen, ref pioSendRequest, ref RecvBuff[0], ref RecvLen);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                return retCode;
            }

            else
            {
                try
                {
                    tmpStr = "";
                    switch (reqType)
                    {
                        case 0:
                            for (indx = (RecvLen - 2); indx <= (RecvLen - 1); indx++)
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                            }

                            if ((tmpStr).Trim() != "90 00")
                            {
                                //MessageBox.Show("Return bytes are not acceptable.");
                                return -202;
                            }

                            break;

                        case 1:

                            for (indx = (RecvLen - 2); indx <= (RecvLen - 1); indx++)
                            {
                                tmpStr = tmpStr + string.Format("{0:X2}", RecvBuff[indx]);
                            }

                            if (tmpStr.Trim() != "90 00")
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                            }

                            else
                            {
                                tmpStr = "ATR : ";
                                for (indx = 0; indx <= (RecvLen - 3); indx++)
                                {
                                    tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                                }
                            }

                            break;

                        case 2:

                            for (indx = 0; indx <= (RecvLen - 1); indx++)
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                            }

                            break;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    return -200;
                }
            }
            return retCode;
        }

        public bool connectCard()
        {
            connActive = true;

            retCode = Card.SCardConnect(hContext, readername, Card.SCARD_SHARE_SHARED,
                      Card.SCARD_PROTOCOL_T0 | Card.SCARD_PROTOCOL_T1, ref hCard, ref Protocol);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                string data = "No Card Present. Please Scan a Card;" + Card.GetScardErrMsg(retCode);
                //need to log this
                //MessageBox.Show(data, "Card not available", MessageBoxButton.OK, MessageBoxImage.Error);
                connActive = false;
                return false;
            }
            return true;
        }
        public string getcardUID()
        {
            string cardUID = "";
            byte[] receivedUID = new byte[256];
            Card.SCARD_IO_REQUEST request = new Card.SCARD_IO_REQUEST();
            request.dwProtocol = Card.SCARD_PROTOCOL_T1;
            request.cbPciLength = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Card.SCARD_IO_REQUEST));
            byte[] sendBytes = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 }; //get UID command      for Mifare cards
            int outBytes = receivedUID.Length;
            int status = Card.SCardTransmit(hCard, ref request, ref sendBytes[0], sendBytes.Length, ref request, ref receivedUID[0], ref outBytes);

            if (status != Card.SCARD_S_SUCCESS)
            {
                cardUID = "Error";
            }
            else
            {
                cardUID = BitConverter.ToString(receivedUID.Take(7).Reverse().ToArray()).ToLower();
                //remove dashes
                cardUID = BitConverter.ToString(receivedUID.Take(7).Reverse().ToArray()).Replace("-", string.Empty).ToLower();
                //get in reverse
                //cardUID = BitConverter.ToString(receivedUID.Take(4).ToArray()).Replace("-", string.Empty).ToLower();
            }

            return cardUID;
        }
        public void SelectDevice()
        {
            List<string> availableReaders = this.ListReaders();
            this.RdrState = new Card.SCARD_READERSTATE();
            if (availableReaders.Count < 0)
            {
                //need to add a log
                //MessageBox.Show("Check your device and please restart again", "Reader not connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            readername = availableReaders[0].ToString();//selecting first device
            this.RdrState.RdrName = readername;
        }
        public List<string> ListReaders()
        {
            int ReaderCount = 0;
            List<string> AvailableReaderList = new List<string>();

            //Make sure a context has been established before 
            //retrieving the list of smartcard readers.
            retCode = Card.SCardListReaders(hContext, null, null, ref ReaderCount);
            if (retCode != Card.SCARD_S_SUCCESS)
            {
                //log this, cant read card
                //MessageBox.Show(Card.GetScardErrMsg(retCode));
                //connActive = false;
            }

            byte[] ReadersList = new byte[ReaderCount];

            //Get the list of reader present again but this time add sReaderGroup, retData as 2rd & 3rd parameter respectively.
            retCode = Card.SCardListReaders(hContext, null, ReadersList, ref ReaderCount);
            if (retCode != Card.SCARD_S_SUCCESS)
            {
                //log this, cant read card
                //MessageBox.Show(Card.GetScardErrMsg(retCode));
            }

            string rName = "";
            int indx = 0;
            if (ReaderCount > 0)
            {
                // Convert reader buffer to string
                while (ReadersList[indx] != 0)
                {

                    while (ReadersList[indx] != 0)
                    {
                        rName = rName + (char)ReadersList[indx];
                        indx = indx + 1;
                    }

                    //Add reader name to list
                    AvailableReaderList.Add(rName);
                    rName = "";
                    indx = indx + 1;

                }
            }
            return AvailableReaderList;

        }
        internal void establishContext()
        {
            retCode = Card.SCardEstablishContext(Card.SCARD_SCOPE_SYSTEM, 0, 0, ref hContext);
            if (retCode != Card.SCARD_S_SUCCESS)
            {
                //log this, cant read card
                //MessageBox.Show("Check your device and please restart again", "Reader not connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                connActive = false;
                return;
            }
        }

        #endregion
    }
}
