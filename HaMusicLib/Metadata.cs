/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaMusicLib
{
    [ProtoContract]
    public class Metadata : PropertyNotifierBase
    {
        // Note - this class is a PropertyNotifierBase to satisfy bindings to it, however, I didn't actually go through the hassle of hooking in
        // the event callbacks (SetProperty etc.) because we are assuming this is a read-only class; Public setters exist on the properties only 
        // to allow Protobuf deserialization.

        [ProtoMember(1, IsRequired = true)]
        public string Album { get; set; }
        [ProtoMember(2, IsRequired = true)]
        public string Artist { get; set; }
        [ProtoMember(3, IsRequired = true)]
        public string Title { get; set; }

        public Metadata()
        {
        }

        public Metadata(TagLib.Tag tag)
        {
            Album = tag.Album;
            Artist = tag.JoinedAlbumArtists;
            Title = tag.Title;
        }
    }
}
