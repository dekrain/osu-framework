﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Input
{
    /// <summary>
    /// A source from which we can retrieve user text input.
    /// Generally hides a native implementation from the game framework.
    /// </summary>
    public class TextInputSource
    {
        /// <summary>
        /// Whether IME is actively providing text composition through <see cref="OnImeComposition"/> and accepting input from the user.
        /// </summary>
        public bool ImeActive { get; private set; }

        private readonly object pendingLock = new object();

        private string pendingText = string.Empty;

        /// <summary>
        /// Counts how many times consumers have activated this <see cref="TextInputSource"/>.
        /// </summary>
        private int activationCounter;

        /// <summary>
        /// Gets all the text that was input by the user since the last <see cref="GetPendingText"/> call.
        /// </summary>
        /// <remarks>
        /// Should be periodically called to collect user-input text.
        /// </remarks>
        public string GetPendingText()
        {
            lock (pendingLock)
            {
                string oldPending = pendingText;
                pendingText = string.Empty;
                return oldPending;
            }
        }

        /// <summary>
        /// Activates this <see cref="TextInputSource"/>.
        /// User text input can be acquired through <see cref="GetPendingText"/>, <see cref="OnImeComposition"/> and <see cref="OnImeResult"/>.
        /// </summary>
        /// <remarks>
        /// Each <see cref="Activate"/> must be followed by a <see cref="Deactivate"/>.
        /// </remarks>
        public void Activate()
        {
            if (Interlocked.Increment(ref activationCounter) == 1)
                ActivateTextInput();
        }

        /// <summary>
        /// Ensures that the native implementation that retrieves user text input is activated
        /// and that the user can start entering text.
        /// </summary>
        public void EnsureActivated()
        {
            if (activationCounter >= 1)
                EnsureTextInputActivated();
        }

        /// <summary>
        /// Deactivates this <see cref="TextInputSource"/>.
        /// </summary>
        /// <remarks>
        /// Should be called once text input is no longer needed.
        /// Each <see cref="Deactivate"/> must be preceded by an <see cref="Activate"/>.
        /// </remarks>
        public void Deactivate()
        {
            if (Interlocked.Decrement(ref activationCounter) == 0)
            {
                DeactivateTextInput();

                lock (pendingLock)
                {
                    // clear out the pending text in case some of it wasn't consumed
                    pendingText = string.Empty;
                }
            }
        }

        /// <summary>
        /// Sets where the native implementation displays IME controls and other text input elements.
        /// </summary>
        /// <param name="rectangle">Should be provided in screen space.</param>
        public virtual void SetImeRectangle(RectangleF rectangle)
        {
        }

        /// <summary>
        /// Resets IME.
        /// This clears the current composition string and prepares it for new input.
        /// </summary>
        public virtual void ResetIme()
        {
            ImeActive = false;
        }

        /// <summary>
        /// Invoked when IME composition starts or changes.
        /// </summary>
        /// <remarks>Empty string for text means that the composition has been cancelled.</remarks>
        public event ImeCompositionDelegate OnImeComposition;

        /// <summary>
        /// Invoked when IME composition successfully completes.
        /// </summary>
        public event Action<string> OnImeResult;

        /// <summary>
        /// Adds <paramref name="text"/> to the text pending to be collected by <see cref="GetPendingText"/>.
        /// </summary>
        /// <remarks>
        /// Used for collecting inputted text from native implementations.
        /// </remarks>
        protected void AddPendingText(string text)
        {
            lock (pendingLock)
            {
                pendingText += text;
            }
        }

        /// <summary>
        /// Activates the native implementation that provides text input.
        /// Should be overriden per-platform.
        /// </summary>
        /// <remarks>
        /// An active native implementation should add user input text with <see cref="AddPendingText"/>.
        /// and forward IME composition events through <see cref="TriggerImeComposition"/> and <see cref="TriggerImeResult"/>.
        /// </remarks>
        protected virtual void ActivateTextInput()
        {
        }

        /// <inheritdoc cref="EnsureActivated"/>
        /// Should be overriden per-platform.
        /// <remarks>
        /// Only called if the native implementation has been activated with <see cref="Activate"/>.
        /// </remarks>
        protected virtual void EnsureTextInputActivated()
        {
        }

        /// <summary>
        /// Deactivates the native implementation that provides text input.
        /// Should be overriden per-platform.
        /// </summary>
        protected virtual void DeactivateTextInput()
        {
        }

        protected void TriggerImeComposition(string text, int start, int length)
        {
            // empty text means that composition isn't active.
            ImeActive = !string.IsNullOrEmpty(text);

            OnImeComposition?.Invoke(text, start, length);
        }

        protected void TriggerImeResult(string text)
        {
            // IME is deactivated / not providing active composition once the current one is finalized.
            ImeActive = false;

            OnImeResult?.Invoke(text);
        }

        /// <summary>
        /// Fired on a new IME composition.
        /// </summary>
        /// <param name="text">The composition text.</param>
        /// <param name="start">The index of the selection start.</param>
        /// <param name="length">The length of the selection.</param>
        /// <remarks>Empty string for <paramref name="text"/> means that the composition has been cancelled.</remarks>
        public delegate void ImeCompositionDelegate(string text, int start, int length);
    }
}
