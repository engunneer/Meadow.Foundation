﻿using Meadow.Hardware;
using Meadow.Units;
using System;
using System.Linq;

namespace Meadow.Foundation.ICs.IOExpanders
{
    /// <summary>
    /// Provide an interface to connect to a MCP3xxx analog to digital converter (ADC)
    /// </summary>
    abstract partial class Mcp3xxx : IAnalogInputController, ISpiPeripheral
    {
        /// <summary>
        /// the number of input channels on the ADC
        /// </summary>
        protected int ChannelCount { get; set; }

        /// <summary>
        /// The default SPI bus speed for the device
        /// </summary>
        public Frequency DefaultSpiBusSpeed => new(10000, Frequency.UnitType.Kilohertz);

        /// <summary>
        /// The SPI bus speed for the device
        /// </summary>
        public Frequency SpiBusSpeed
        {
            get => spiComms.BusSpeed;
            set => spiComms.BusSpeed = value;
        }

        /// <summary>
        /// The default SPI bus mode for the device
        /// </summary>
        public SpiClockConfiguration.Mode DefaultSpiBusMode => SpiClockConfiguration.Mode.Mode0;

        /// <summary>
        /// The SPI bus mode for the device
        /// </summary>
        public SpiClockConfiguration.Mode SpiBusMode
        {
            get => spiComms.BusMode;
            set => spiComms.BusMode = value;
        }

        /// <summary>
        /// The resolution of the analog-to-digital converter in the Mcp3xxx
        /// This is model-specific and not confiruable 
        /// </summary>
        public int AdcResolutionInBits { get; protected set; }

        /// <summary>
        /// The maximum raw value returned by the ADC
        /// </summary>
        internal int AdcMaxValue { get; set; }

        private readonly ISpiCommunications spiComms;

        /// <summary>
        /// Mcp3xxx base class contructor
        /// </summary>
        /// <param name="spiBus">The SPI bus</param>
        /// <param name="chipSelectPin">Chip select pin</param>
        /// <param name="channelCount">The number of input channels</param>   
        /// <param name="adcResolutionInBits">The resolution in bits for the ADC</param>
        protected Mcp3xxx(ISpiBus spiBus,
            IPin chipSelectPin,
            int channelCount, int adcResolutionInBits) :
            this(spiBus, chipSelectPin.CreateDigitalOutputPort(), channelCount, adcResolutionInBits)
        {
        }

        /// <summary>
        /// Mcp3xxx base class contructor
        /// </summary>
        /// <param name="spiBus">The SPI bus</param>
        /// <param name="chipSelectPort">Chip select port</param>
        /// <param name="channelCount">The number of input channels</param>   
        /// <param name="adcResolutionInBits">The resolution in bits for the ADC</param>
        protected Mcp3xxx(ISpiBus spiBus,
            IDigitalOutputPort chipSelectPort,
            int channelCount, int adcResolutionInBits)
        {
            AdcResolutionInBits = adcResolutionInBits;
            AdcMaxValue = (int)Math.Pow(2, adcResolutionInBits);

            ChannelCount = channelCount;

            spiComms = new SpiCommunications(spiBus, chipSelectPort, DefaultSpiBusSpeed, DefaultSpiBusMode);
        }

        /// <summary>
        /// Create an analog input port for a pin
        /// </summary>
        /// <param name="pin">The pin to use for the analog input port</param>
        /// <param name="sampleCount">The number of samples to take when measuring the pin's voltage</param>
        /// <returns>An instance of <see cref="IAnalogInputPort"/> that represents the analog input on the specified pin</returns>
        protected IAnalogInputPort CreateAnalogInputPort(IPin pin, int sampleCount = 64)
        {
            return CreateAnalogInputPort(pin, sampleCount, TimeSpan.FromSeconds(1), new Voltage(0));
        }

        /// <summary>
        /// Creates a new instance of an `IAnalogInputPort` for the specified pin
        /// </summary>
        /// <param name="pin">The IPin object that this port is created from</param>
        /// <param name="sampleCount">The number of samples to take</param>
        /// <param name="sampleInterval">The interval delay between samples</param>
        /// <param name="voltageReference">The `Voltage` reference for ADC readings</param>
        /// <returns>A new instance of an `IAnalogInputPort`</returns>
        public IAnalogInputPort CreateAnalogInputPort(IPin pin,
            int sampleCount,
            TimeSpan sampleInterval,
            Voltage voltageReference)
        {
            InputType inputType = InputType.SingleEnded;

            if (IsInputTypeSupported(inputType) == false)
            {
                inputType = InputType.Differential;
            }

            return CreateAnalogInputPort(pin, sampleCount, sampleInterval, voltageReference, inputType);
        }

        /// <summary>
        /// Creates a new instance of an `IAnalogInputPort` for the specified pin
        /// </summary>
        /// <param name="pin">The IPin object that this port is created from</param>
        /// <param name="sampleCount">The number of samples to take</param>
        /// <param name="sampleInterval">The interval delay between samples</param>
        /// <param name="voltageReference">The `Voltage` reference for ADC readings</param>
        /// <param name="inputType">The pin channel input type</param>
        /// <returns>A new instance of an `IAnalogInputPort`</returns>
        public IAnalogInputPort CreateAnalogInputPort(IPin pin,
            int sampleCount,
            TimeSpan sampleInterval,
            Voltage voltageReference,
            InputType inputType)
        {
            if (IsInputTypeSupported(inputType) == false)
            {
                throw new Exception($"InputType {inputType} is not supported");
            }

            var channel = pin.SupportedChannels.OfType<IAnalogChannelInfo>().FirstOrDefault();

            return channel == null
                ? throw new NotSupportedException($"Pin {pin.Name} Does not support ADC")
                : (IAnalogInputPort)new AnalogInputPort(this, pin, channel, sampleCount, inputType);
        }

        /// <summary>
        /// Is the input type supported on this MCP3xxx version
        /// </summary>
        /// <param name="inputType">The input type</param>
        /// <returns>True if supported, false if not supported</returns>
        public virtual bool IsInputTypeSupported(InputType inputType)
        {
            return true;
        }

        /// <summary>
        /// Checks if channel is in range
        /// </summary>
        /// <param name="channel">The channel</param>
        /// <param name="channelCount">The number of channels on the device</param>
        protected void ValidateChannel(int channel, int channelCount)
        {
            if (channel < 0 || channel >= channelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), $"ADC channel must be within the range 0-{channelCount - 1}.");
            }
        }

        /// <summary>
        /// Checks if the channel is in range of the available input channels and that both channels are part of a valid pairing
        /// </summary>
        /// <param name="valueChannel">The 1st (value) channel in the pairing</param>
        /// <param name="referenceChannel">The 2nd (reference) channel in the pairing</param>
        protected void ValidateChannelPairing(int valueChannel, int referenceChannel)
        {
            ValidateChannel(valueChannel, ChannelCount);
            ValidateChannel(referenceChannel, ChannelCount);

            if (valueChannel / 2 != referenceChannel / 2 || valueChannel == referenceChannel)
            {
                throw new ArgumentException($"ADC differential channels must be unique and part of the same channel pairing: {nameof(valueChannel)} - {nameof(referenceChannel)}");
            }
        }

        /// <summary>
        /// Reads a value from the device for a single ended input
        /// </summary>
        /// <param name="channel">Channel which represents the input signal</param>
        /// <returns>The raw voltage</returns>
        protected virtual int ReadSingleEnded(int channel)
        {
            ValidateChannel(channel, ChannelCount);

            return ReadInternal(channel, InputType.SingleEnded, AdcResolutionInBits);
        }

        /// <summary>
        /// Reads a value from the device using pseudo-differential inputs
        /// </summary>
        /// <param name="valueChannel">Channel which represents the signal</param>
        /// <param name="referenceChannel">Channel which represents ground</param>
        /// <returns>The raw relative voltage</returns>
        protected virtual int ReadPseudoDifferential(int valueChannel, int referenceChannel)
        {
            ValidateChannelPairing(valueChannel, referenceChannel);

            return ReadInternal(channel: valueChannel,
                valueChannel > referenceChannel ? InputType.InvertedDifferential : InputType.Differential,
                AdcResolutionInBits);
        }

        /// <summary>
        /// Reads a value from the device using differential inputs
        /// </summary>
        /// <param name="valueChannel">Channel which represents the positive signal</param>
        /// <param name="referenceChannel">Channel which represents the negative signal</param>
        /// <returns>The raw relative voltage</returns>
        protected virtual int ReadDifferential(int valueChannel, int referenceChannel)
        {
            ValidateChannel(valueChannel, ChannelCount);
            ValidateChannel(referenceChannel, ChannelCount);

            if (valueChannel == referenceChannel)
            {
                throw new ArgumentException($"ADC differential channels must be unique: {nameof(valueChannel)} - {nameof(referenceChannel)}", nameof(valueChannel));
            }

            return ReadInternal(valueChannel, InputType.SingleEnded, AdcResolutionInBits) -
                   ReadInternal(referenceChannel, InputType.SingleEnded, AdcResolutionInBits);
        }

        /// <summary>
        /// Reads a value from the device
        /// </summary>
        /// <param name="channel">Channel to read - for diffential inputs this represents a channel pair (valid values: 0 - channelcount - 1 or 0 - channelcount / 2 - 1  with differential inputs)</param>
        /// <param name="inputType">The type of input channel to read</param>
        /// <param name="adcResolutionBits">The number of bits in the returned value</param>
        /// <returns>A value corresponding to relative voltage level on specified device channel</returns>
        protected int ReadInternal(int channel, InputType inputType, int adcResolutionBits)
        {
            ValidateChannel(channel, ChannelCount);

            int requestVal = ChannelCount switch
            {
                4 or 8 => (inputType == InputType.SingleEnded ? 0b1_1000 : 0b1_0000) | channel,
                2 => (inputType == InputType.SingleEnded ? 0b1101 : 0b1001) | channel << 1,
                1 => 0,
                _ => throw new InvalidOperationException($"Unsupported ChannelCount {ChannelCount}"),
            };

            return ReadInternalRaw(requestVal, adcResolutionBits, ChannelCount > 2 ? 1 : 0);
        }

        /// <summary>
        /// Reads a value from the device
        /// </summary>
        /// <param name="adcRequest">A bit pattern to be sent to the ADC</param>
        /// <param name="adcResolutionInBits">The number of bits in the returned value</param>
        /// <param name="delayBits">The number of bits to be delayed between the request and the response being read</param>
        /// <returns>A value corresponding to a voltage level on the input pin described by the request</returns>
        protected int ReadInternalRaw(int adcRequest, int adcResolutionInBits, int delayBits)
        {
            int returnValue = 0;
            int bufferSize;

            adcRequest <<= (adcResolutionInBits + delayBits + 1);

            bufferSize = (adcRequest & 0x00FF0000) != 0 ? 3 : 2;

            Span<byte> requestBuffer = stackalloc byte[bufferSize];
            Span<byte> responseBuffer = stackalloc byte[bufferSize];

            // take the request and put it in a byte array
            for (int i = 0; i < bufferSize; i++)
            {
                requestBuffer[i] = (byte)(adcRequest >> (bufferSize - i - 1) * 8);
            }

            spiComms.Exchange(requestBuffer, requestBuffer);

            // copy the response from the ADC to the return value
            for (int i = 0; i < bufferSize; i++)
            {
                returnValue <<= 8;
                returnValue += responseBuffer[i];
            }

            // test the response from the ADC to verify the null bit is 0
            if ((returnValue & (1 << adcResolutionInBits)) != 0)
            {
                throw new InvalidOperationException("Invalid data was read from the sensor");
            }

            // return the ADC response with any possible higher bits masked out
            return returnValue & (1 << adcResolutionInBits) - 1;
        }
    }
}