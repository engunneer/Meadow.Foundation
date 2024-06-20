﻿using Meadow.Hardware;
using Meadow.Units;
using System;

namespace Meadow.Foundation.Servos;

/// <summary>
/// Represents the base class for a servo, implementing common functionality.
/// </summary>
public abstract class ServoBase : IServo, IDisposable
{
    private bool portCreated = false;

    /// <inheritdoc/>
    public abstract void Neutral();

    /// <summary>
    /// Gets a value indicating whether the servo has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets the PWM port used to control the servo.
    /// </summary>
    protected IPwmPort PwmPort { get; }

    /// <inheritdoc/>
    public TimeSpan TrimOffset { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServoBase"/> class with a specified PWM pin and frequency.
    /// </summary>
    /// <param name="pwmPin">The pin used for PWM control.</param>
    /// <param name="pwmFrequency">The frequency of the PWM signal.</param>
    protected ServoBase(IPin pwmPin, Frequency pwmFrequency)
    {
        if (!pwmPin.Supports<IPwmChannelInfo>())
        {
            throw new ArgumentException("Pin does not support PWM functionality");
        }

        PwmPort = pwmPin.CreatePwmPort(pwmFrequency, 0, false);
        portCreated = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServoBase"/> class with a specified PWM port.
    /// </summary>
    /// <param name="pwmPort">The PWM port used for control.</param>
    protected ServoBase(IPwmPort pwmPort)
    {
        PwmPort = pwmPort;
    }

    /// <inheritdoc/>
    public virtual void Disable()
    {
        PwmPort.Stop();
    }

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing && portCreated)
            {
                PwmPort?.Dispose();
            }

            IsDisposed = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private double CalculateDutyCycle(TimeSpan pulseDuration)
    {
        return pulseDuration.TotalSeconds * PwmPort.Frequency.Hertz / 2d;
    }

    /// <summary>
    /// Send a command pulse
    /// </summary>
    /// <param name="pulseDuration">The pulse duration</param>
    protected virtual void SendCommandPulseWithTrim(TimeSpan pulseDuration)
    {
        var duty = CalculateDutyCycle(pulseDuration + TrimOffset);
        Console.WriteLine($"Duration of: {pulseDuration} is a duty of {duty}");
        PwmPort.DutyCycle = duty;
    }

}