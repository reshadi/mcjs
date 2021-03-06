// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System;

using m.Util.Diagnose;

namespace mjr.Builtins.TypedArray
{
    class JSFloat64Array : JSTypedArrayBase
    {
        private const int TypeSize = sizeof(Int64);

        public JSFloat64Array()
            : base(new mdr.DObject(), "Float64Array", TypeSize)
        {
            JittedCode = (ref mdr.CallFrame callFrame) =>
            {
                DFloat64Array float64array = new DFloat64Array(TargetPrototype, 0, TypeSize);
                var argsCount = callFrame.PassedArgsCount;

                if (argsCount == 1)
                {
                    switch (callFrame.Arg0.ValueType)
                    {
                        case mdr.ValueTypes.Int32:
                            float64array = new DFloat64Array(TargetPrototype, callFrame.Arg0.AsInt32() * TypeSize, TypeSize);
                            break;

                        case mdr.ValueTypes.Object:
                            float64array = createArrayFromObject(callFrame.Arg0.AsDObject());
                            break;

                        case mdr.ValueTypes.Array:
                            mdr.DArray array = callFrame.Arg0.AsDArray();
                            float64array = new DFloat64Array(TargetPrototype, array.Length * TypeSize, TypeSize);
                            fillArray(float64array, array);
                            break;

                        default:
                            Trace.Fail("invalid Arguments");
                            break;
                    }
                }
                if (argsCount == 2)
                {
                    int byteoffset = callFrame.Arg1.AsInt32();
                    float64array = createArrayFromObject(callFrame.Arg0.AsDObject(), byteoffset);
                }
                if (argsCount == 3)
                {
                    var byteoffset = callFrame.Arg1.AsInt32();
                    var length = callFrame.Arg2.AsInt32();
                    checkOffsetMemBoundary(byteoffset);
                    float64array = createArrayFromObject(callFrame.Arg0.AsDObject(), byteoffset, length * TypeSize);
                }

                if (IsConstrutor)
                    callFrame.This = (float64array);
                else
                    callFrame.Return.Set(float64array);
            };

            TargetPrototype.DefineOwnProperty("subarray", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                DFloat64Array array = (callFrame.This as DFloat64Array);
                var argsCount = callFrame.PassedArgsCount;
                var begin = (argsCount >= 1) ? callFrame.Arg0.AsInt32() : 0;
                var end = array.ByteLength / array.TypeSize;
                end = (argsCount >= 2) ? callFrame.Arg1.AsInt32() : end;
                begin = begin < 0 ? begin += array.ByteLength / array.TypeSize : begin;
                end = end < 0 ? end += array.ByteLength / array.TypeSize : end;
                var bytelength = Math.Max(0, (end - begin)) * array.TypeSize;
                end = Math.Max(array.ByteLength, bytelength);
                var float64array = new DFloat64Array(TargetPrototype, bytelength, array.TypeSize);
                int offset = (begin <= 0) ? 0 : begin * array.TypeSize;
                for (int i = 0; i < float64array.ByteLength; ++i)
                    float64array.Elements_[i] = array.Elements_[i + offset];
                callFrame.Return.Set(float64array);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
        }

        private DFloat64Array createArrayFromObject(mdr.DObject array, int byteoffset = 0, int bytelength = 0)
        {
            var buffer = array as DArrayBuffer;
            if (buffer != null)
            {
                bytelength = (bytelength > 0) ? bytelength : buffer.ByteLength - byteoffset;
                checkOffsetCompatibility(byteoffset, bytelength);
                return new DFloat64Array(TargetPrototype, buffer, byteoffset, bytelength, TypeSize);
            }

            var darray = array as DTypedArray;
            if (darray != null)
            {
                bytelength = (bytelength > 0) ? bytelength : darray.ByteLength / darray.TypeSize * TypeSize;
                checkOffsetCompatibility(byteoffset, bytelength);
                DFloat64Array tarray = new DFloat64Array(TargetPrototype, bytelength, TypeSize);
                fillArray(tarray, darray);
                return tarray;
            }

            Trace.Fail("invalid Arguments");
            return null;
        }

        private void fillArray(DFloat64Array dst, mdr.DArrayBase src)
        {
            for (int i = 0; i < dst.ByteLength / dst.TypeSize; ++i)
            {
                double value = src.GetPropertyDescriptor(i).Get(src).AsDouble();
                dst.AddPropertyDescriptor(i).Set(dst, value);
            }
        }
    }
}
