using Meadow.Hardware;

namespace Meadow.Foundation.Displays
{
    /// <summary>
    /// Represents an SSD1675 ePaper mono display
    /// </summary>
    public class Ssd1675 : EPaperMonoBase
    {

        /// <summary>
        /// Create a new Ssd1675 object
        /// </summary>
        /// <param name="spiBus">SPI bus connected to display</param>
        /// <param name="chipSelectPin">Chip select pin</param>
        /// <param name="dcPin">Data command pin</param>
        /// <param name="resetPin">Reset pin</param>
        /// <param name="busyPin">Busy pin</param>
        /// <param name="width">Width of display in pixels</param>
        /// <param name="height">Height of display in pixels</param>
        public Ssd1675(ISpiBus spiBus, IPin chipSelectPin, IPin dcPin, IPin resetPin, IPin busyPin,
            int width, int height) :
            base(spiBus, chipSelectPin, dcPin, resetPin, busyPin, width, height)
        { }

        /// <summary>
        /// Create a new Ssd1675 ePaper display object
        /// </summary>
        /// <param name="spiBus">SPI bus connected to display</param>
        /// <param name="chipSelectPort">Chip select output port</param>
        /// <param name="dataCommandPort">Data command output port</param>
        /// <param name="resetPort">Reset output port</param>
        /// <param name="busyPort">Busy input port</param>
        /// <param name="width">Width of display in pixels</param>
        /// <param name="height">Height of display in pixels</param>
        public Ssd1675(ISpiBus spiBus,
            IDigitalOutputPort chipSelectPort,
            IDigitalOutputPort dataCommandPort,
            IDigitalOutputPort resetPort,
            IDigitalInputPort busyPort,
            int width, int height) :
            base(spiBus, chipSelectPort, dataCommandPort, resetPort, busyPort, width, height)
        { }

        /// <summary>
        /// Initialize the display
        /// </summary>
        protected override void Initialize()
        {
            Reset();

            DelayMs(100);

            WaitUntilIdle();
            SendCommand(SSD1675_SW_RESET);
            WaitUntilIdle();

			SendCommand(SSD1675_SET_ANALOGBLOCK);
			SendData(0x54);
			SendCommand(SSD1675_SET_DIGITALBLOCK);
			SendData(0x3B);

			SendCommand(SSD1675_DRIVER_CONTROL);
			SendData(0xFA);
			SendData(0x01);
            SendData(0x00);

            SendCommand(SSD1675_DATA_MODE);
            SendData(0x03);

            //set window
            SendCommand(SSD1675_SET_RAMXPOS);
            SendData(0x00);
            SendData(Width / 8);

            SendCommand(SSD1675_SET_RAMYPOS);
            SendData(0x00);
            SendData(0x00);
            SendData(Height);
            SendData(Height >> 8);

            //set cursor
            SetRamAddress();

            SendCommand(SSD1675_WRITE_BORDER);
            SendData(0x03);

            SendCommand(SSD1675_WRITE_VCOM);
            SendData(0x70);

			SendCommand(SSD1675_GATE_VOLTAGE);
			SendData(0x80);
			SendData(0x80);
			SendCommand(SSD1675_SOURCE_VOLTAGE);
			SendData(0x80);
			SendData(0x80);
			SendData(0x80);
			SendData(0x80);
			SendCommand(SSD1675_WRITE_DUMMY);
			SendData(0x80);
			SendData(0x80);
			SendCommand(SSD1675_WRITE_LUT);
			// TODO: 71 bytes
            //SendData(0x80);
			SendCommand(SSD1675_SET_RAMXCOUNT);
			SendData(Width / 8);
			SendCommand(SSD1675_SET_RAMYCOUNT);
			SendData(Height);
			SendData(Height >> 8);

			WaitUntilIdle();
        }


        /// <summary>
        /// Send the display buffer to the display and refresh
        /// </summary>
        public override void Show(int left, int top, int right, int bottom)
        {
            Show();
        }

        /// <summary>
        /// Send the display buffer to the display and refresh
        /// </summary>
        public override void Show()
        {
            DisplayFrame(imageBuffer.Buffer);
        }

        /// <summary>
        /// Clear the on-display frame buffer
        /// </summary>
        protected void ClearFrame()
        {
            SetRamAddress();

            SendCommand(SSD1675_WRITE_RAM1);

            for (int i = 0; i < Width * Height / 8; i++)
            {
                SendData(0xFF);
            }

            SetRamAddress();

            SendCommand(SSD1675_WRITE_RAM2);
            for (int i = 0; i < Width * Height / 8; i++)
            {
                SendData(0xFF);
            }
        }

        void DisplayFrame(byte[] blackBuffer)
        {
            SetRamAddress();
            SendCommand(SSD1675_WRITE_RAM1);
            SendData(blackBuffer);

            DisplayFrame();
        }

        void DisplayFrame()
        {
            SendCommand(SSD1675_DISP_CTRL2);
            SendData(0xF4);

            SendCommand(SSD1675_MASTER_ACTIVATE);

            WaitUntilIdle();
        }

        void SetRamAddress()
        {
            SendCommand(SSD1675_SET_RAMXCOUNT);
            SendData(0x00);

            SendCommand(SSD1675_SET_RAMYCOUNT);
            SendData(0x00);
            SendData(0x00);
        }

        void PowerDown()
        {
            SendCommand(SSD1675_DEEP_SLEEP);
            SendData(0x01);

            DelayMs(100);
        }

        const byte SSD1675_DRIVER_CONTROL = 0x01;
        const byte SSD1675_GATE_VOLTAGE = 0x03;
        const byte SSD1675_SOURCE_VOLTAGE = 0x04;
        const byte SSD1675_DEEP_SLEEP = 0x10;
        const byte SSD1675_DATA_MODE = 0x11;
		const byte SSD1675_SW_RESET = 0x12;
        const byte SSD1675_HV_READY = 0x14;
		const byte SSD1675_VCI_READY = 0x15;
        const byte SSD1675_TEMP_WRITE = 0x1A;
        const byte SSD1675_MASTER_ACTIVATE = 0x20;
        const byte SSD1675_DISP_CTRL1 = 0x21;
        const byte SSD1675_DISP_CTRL2 = 0x22;
        const byte SSD1675_WRITE_RAM1 = 0x24;
        const byte SSD1675_WRITE_RAM2 = 0x26;
        const byte SSD1675_WRITE_VCOM = 0x2C;
        const byte SSD1675_READ_OTP = 0x2D;
		const byte SSD1675_WRITE_LUT = 0x32;
        const byte SSD1675_WRITE_DUMMY = 0x3A;
        const byte SSD1675_WRITE_GATELINE = 0x3B;
        const byte SSD1675_WRITE_BORDER = 0x3C;
        const byte SSD1675_SET_RAMXPOS = 0x44;
        const byte SSD1675_SET_RAMYPOS = 0x45;
        const byte SSD1675_SET_RAMXCOUNT = 0x4E;
		const byte SSD1675_SET_RAMYCOUNT = 0x4F;
        const byte SSD1675_SET_ANALOGBLOCK = 0x74;
		const byte SSD1675_SET_DIGITALBLOCK = 0x7E;
    }
}