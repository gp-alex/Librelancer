﻿/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyNodeLibrary
	{
		enum AleTypes {
			Boolean = 0x001,
			Integer = 0x002,
			Float = 0x003,
			Name = 0x103,
			IPair = 0x104,
			Transform = 0x105,
			FloatAnimation = 0x200,
			ColorAnimation = 0x201,
			CurveAnimation = 0x202
		}

		public float Version;
		public List<AlchemyNode> Nodes = new List<AlchemyNode> ();
		public AlchemyNodeLibrary (LeafNode utfleaf)
		{
			using (var reader = new BinaryReader (new MemoryStream (utfleaf.ByteArrayData))) {
				Version = reader.ReadSingle ();
				int nodeCount = reader.ReadInt32 ();
				for (int nc = 0; nc < nodeCount; nc++) {
					ushort nameLen = reader.ReadUInt16 ();
					var nodeName = Encoding.ASCII.GetString (reader.ReadBytes (nameLen)).TrimEnd ('\0');
					reader.BaseStream.Seek(nameLen & 1, SeekOrigin.Current); //padding
					var node = new AlchemyNode () { Name = nodeName };
					node.CRC = CrcTool.FLAleCrc(nodeName);
					uint id, crc;
					while (true) {
						id = reader.ReadUInt16 ();
						if (id == 0)
							break;
						AleTypes type = (AleTypes)(id & 0x7FFF);
						crc = reader.ReadUInt32 ();
						string efname;
						if (!AleCrc.FxCrc.TryGetValue (crc, out efname)) {
							efname = string.Format ("CRC: 0x{0:X}", crc);
						}
						object value = null;
						switch (type) {
						case AleTypes.Boolean:
							value = (id & 0x8000) != 0 ? true : false;
							break;
						case AleTypes.Integer:
							value = reader.ReadUInt32 ();
							break;
						case AleTypes.Float:
							value = reader.ReadSingle ();
							break;
						case AleTypes.Name:
							var vallen = reader.ReadUInt16 ();
							if (vallen != 0)
								value = Encoding.ASCII.GetString (reader.ReadBytes (vallen)).TrimEnd ('\0');
							reader.BaseStream.Seek(vallen & 1, SeekOrigin.Current); //padding
							break;
						case AleTypes.IPair:
							value = new Tuple<uint,uint> (reader.ReadUInt32 (), reader.ReadUInt32 ());
							break;
						case AleTypes.Transform:
							value = new AlchemyTransform (reader);
							break;
						case AleTypes.FloatAnimation:
							value = new AlchemyFloatAnimation (reader);
							break;
						case AleTypes.ColorAnimation:
							value = new AlchemyColorAnimation (reader);
							break;
						case AleTypes.CurveAnimation:
							value = new AlchemyCurveAnimation (reader);
							break;
						default:
							throw new InvalidDataException ("Invalid ALE Type: 0x" + (id & 0x7FFF).ToString ("x"));
						}
						node.Parameters.Add (new AleParameter () { Name = efname, Value = value });
					}
					AleParameter temp;
					if (node.TryGetParameter("Node_Name", out temp))
					{
						var nn = (string)temp.Value;
						node.CRC = CrcTool.FLAleCrc(nn);
					}
					Nodes.Add (node);
				}
			}
		}
	}
}

