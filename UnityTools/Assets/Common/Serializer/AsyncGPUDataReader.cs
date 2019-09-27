﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityTools.Common
{
    public class AsyncGPUDataReader: MonoBehaviour
    {
        [Serializable]
        protected class FrameData
        {
            public AsyncGPUReadbackRequest readback;
        }

        protected Queue<FrameData> frameQueue = new Queue<FrameData>();

        public void QueueFrame(RenderTexture source)
        {
            this.frameQueue.Enqueue(new FrameData() { readback = AsyncGPUReadback.Request(source) });
        }
        public void QueueFrame(ComputeBuffer source)
        {
            this.frameQueue.Enqueue(new FrameData() { readback = AsyncGPUReadback.Request(source) });
        }

        public void NoneSequential(RenderTexture source)
        {
            AsyncGPUReadback.Request(source, 0, this.OnSuccessed);
        }
        public void NoneSequential(ComputeBuffer source)
        {
            AsyncGPUReadback.Request(source, this.OnSuccessed);
        }

        protected virtual void OnSuccessed(AsyncGPUReadbackRequest readback)
        {
            //get data like this
            //var data = readback.GetData<byte>();
        }
        protected void ProcessQueue()
        {
            while (frameQueue.Count > 0)
            {
                var frame = frameQueue.Peek();

                // Edit mode: Wait for readback completion every frame.
                if (!Application.isPlaying) frame.readback.WaitForCompletion();

                // Skip error frames.
                if (frame.readback.hasError)
                {
                    Debug.LogWarning("GPU readback error was detected.");
                    frameQueue.Dequeue();
                    continue;
                }

                // Break when found a frame that hasn't been read back yet.
                if (!frame.readback.done) break;
                
                // Feed the frame data to the sender. It encodes/sends the
                // frame asynchronously.
                this.OnSuccessed(frame.readback);
                // Done. Remove the frame from the queue.
                frameQueue.Dequeue();
            }
        }
        protected virtual void Update()
        {
            this.ProcessQueue();
        }
    }
}