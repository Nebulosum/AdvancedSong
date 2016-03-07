using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ASong
{
    class TrackEditor
    {
        private string rom;
        private uint trackOffset;
        private List<Command> trackData;
        private int originalTrackSize;

        public class TrackSavedEventArgs
        {
            public bool Repointed;
            public uint OldOffset, NewOffset;

            public TrackSavedEventArgs()
            {
                Repointed = false;
                OldOffset = 0;
                NewOffset = 0;
            }
        }
        public delegate void TrackSavedEventHandler(TrackSavedEventArgs tse);
        public event TrackSavedEventHandler TrackSaved;

        public TrackEditor(string romPath, uint offset)
        {
            trackOffset = offset;
            trackData = new List<Command>();
            rom = romPath;
            originalTrackSize = 0;

            LoadTrack();
            originalTrackSize = GetTrackSize(); // Will be used for saving (check for repoint needed!)
        }

        public List<Command> GetTrackData()
        {
            return trackData;
        }

        private void LoadTrack()
        {
            // Read the data
            BinaryReader br = new BinaryReader(File.OpenRead(rom));
            br.BaseStream.Seek(trackOffset, SeekOrigin.Begin);

            List<Command> commands = new List<Command>();
            bool exit = false;
            while (!exit && br.BaseStream.Position < br.BaseStream.Length)
            {
                byte cmd = br.ReadByte();
                Command command = null;

                #region Read Command
                //var listItem = new ListViewItem();
                //listItem.Text = Disassembler.GetCommandName(cmd);
                if (cmd < 0x80)
                {
                    Repeat repeat = new Repeat();
                    repeat.RelativeOffset = (uint)br.BaseStream.Position - trackOffset;
                    //listItem.SubItems.Add("--");
                    //listItem.BackColor = Color.ForestGreen;
                    if (PeekByte(br) < 0x80)
                    {
                        //listItem.SubItems.Add(((NoteKeys)cmd).ToString() +
                        //            ", " + ((NoteVelocity)br.ReadByte()).ToString());
                        repeat.Arguments.Add(cmd);
                        repeat.Arguments.Add(br.ReadByte());
                    }
                    else
                    {
                        //listItem.SubItems.Add(((NoteKeys)cmd).ToString());
                        repeat.Arguments.Add(cmd);
                    }
                    command = repeat;
                }
                else if (cmd > 0xCF)
                {
                    //listItem.SubItems.Add(cmd.ToString("X2"));
                    //listItem.BackColor = Color.ForestGreen;
                    Generic g = new Generic(cmd);
                    g.RelativeOffset = (uint)br.BaseStream.Position - trackOffset;
                    byte[] peek = PeekBytes(br, 3);
                    if (peek[0] < 0x80)
                    {
                        if (peek[1] < 0x80)
                        {
                            if (peek[2] < 0x80) // Not sure if there even is a third.
                            {
                                //cmd.parameters = new uint[] { br.ReadByte(), br.ReadByte(), br.ReadByte() };
                                //listItem.SubItems.Add(br.ReadByte().ToString("X2") +
                                //    ", " + br.ReadByte().ToString("X2") +
                                //    ", " + br.ReadByte().ToString("X2"));
                                g.Arguments.Add(br.ReadByte());
                                g.Arguments.Add(br.ReadByte());
                                g.Arguments.Add(br.ReadByte());
                            }
                            else
                            {
                                //cmd.parameters = new uint[] { br.ReadByte(), br.ReadByte() };
                                //listItem.SubItems.Add(((NoteKeys)br.ReadByte()).ToString() +
                                //    ", " + ((NoteVelocity)br.ReadByte()).ToString());
                                g.Arguments.Add(br.ReadByte());
                                g.Arguments.Add(br.ReadByte());
                            }
                        }
                        else
                        {
                            //cmd.parameters = new uint[] { br.ReadByte() };
                            //listItem.SubItems.Add(((NoteKeys)br.ReadByte()).ToString());
                            g.Arguments.Add(br.ReadByte());
                        }
                    }
                    command = g;
                }
                else
                {
                    //listItem.SubItems.Add(cmd.ToString("X2"));
                    //listItem.BackColor = (cmd < 0xB1 ? Color.ForestGreen : Color.Orange);
                    switch (cmd)
                    {
                        case 0xB1:
                            command = new Command(cmd);
                            command.RelativeOffset = (uint)br.BaseStream.Position - trackOffset;
                            exit = true;
                            break;

                        case 0xB2:
                        case 0xB3:
                            {
                                //exit = true;
                                //listItem.SubItems.Add((br.ReadUInt32() - 0x8000000).ToString("X"));
                                //listItem.BackColor = Color.Red;
                                Jump j = new Jump(cmd);
                                j.RelativeOffset = (uint)br.BaseStream.Position - trackOffset;
                                j.Pointer = br.ReadUInt32() - 0x08000000;
                                j.RelativePointer = j.Pointer - trackOffset;
                                command = j;
                            }
                            break;

                        case 0xB9:
                        case 0xBA:
                        case 0xBB:
                        case 0xBC:
                        case 0xBD:
                        case 0xBE:
                        case 0xBF:
                        case 0xC0:
                        case 0xC1:
                        case 0xC2:
                        case 0xC3:
                        case 0xC4:
                        case 0xC5:
                            //listItem.SubItems.Add(br.ReadByte().ToString("X2"));
                            {
                                Generic g = new Generic(cmd);
                                g.RelativeOffset = (uint)br.BaseStream.Position - trackOffset;
                                g.Arguments.Add(br.ReadByte());
                                command = g;
                            }
                            break;

                        case 0xC8:
                            //listItem.SubItems.Add(br.ReadByte().ToString("X2"));
                            {
                                Generic g = new Generic(cmd);
                                g.RelativeOffset = (uint)br.BaseStream.Position - trackOffset;
                                g.Arguments.Add(br.ReadByte());
                                command = g;
                            }
                            break;

                        case 0xCD:
                            //listItem.SubItems.Add(br.ReadUInt32().ToString("X"));
                            //listItem.SubItems.Add("Help! itari needs info. on this command!");
                            {
                                command = new xCommand(cmd, br.ReadUInt32());
                                command.RelativeOffset = (uint)br.BaseStream.Position - 4 - trackOffset;
                            }
                            break;
                        case 0xCE:
                        case 0xCF: // Variable parameters (Tie, End Tie)
                            {
                                Generic g = new Generic(cmd);
                                g.RelativeOffset = (uint)br.BaseStream.Position - trackOffset;
                                byte[] peek = PeekBytes(br, 2);
                                if (peek[0] < 0x80)
                                {
                                    if (peek[1] < 0x80)
                                    {
                                        //listItem.SubItems.Add(((NoteKeys)br.ReadByte()).ToString() +
                                        //    ", " + ((NoteVelocity)br.ReadByte()).ToString());
                                        g.Arguments.Add(br.ReadByte());
                                        g.Arguments.Add(br.ReadByte());
                                    }
                                    else
                                    {
                                        //listItem.SubItems.Add(((NoteKeys)br.ReadByte()).ToString());
                                        g.Arguments.Add(br.ReadByte());
                                    }
                                }
                                command = g;
                            }
                            break;

                        default:
                            {
                                command = new Command(cmd);
                                command.RelativeOffset = (uint)br.BaseStream.Position - trackOffset;
                            }
                            break;
                    }
                }

                //listView1.Items.Add(listItem);
                #endregion

                // This way, we can have errors and not crash.
                if (command != null)
                {
                    commands.Add(command);
                }
            }

            br.Dispose();

            trackData.Clear();
            trackData.AddRange(commands);
        }

        // Calculates size of track data in bytes.
        private int GetTrackSize()
        {
            int size = 0;
            foreach (Command cmd in trackData)
            {
                // A bunch of annoying stuff.
                if (cmd is Repeat) size += ((Repeat)cmd).GetSize();
                else if (cmd is Generic) size += ((Generic)cmd).GetSize();
                else if (cmd is Jump) size += ((Jump)cmd).GetSize();
                else if (cmd is xCommand) size += ((xCommand)cmd).GetSize();
                else size += cmd.GetSize();
            }
            return size;
        }

        private bool SaveTrack()
        {
            if (trackData.Count == 0) return false;

            // Open ROM for writing
            GBABinaryWriter bw = new GBABinaryWriter(File.OpenWrite(rom));
            bw.BaseStream.Seek(trackOffset, SeekOrigin.Begin);

            // Overwrite track with FF, incase we repoint or something silly like that.
            for (int i = 0; i < originalTrackSize; i++)
            {
                bw.Write((byte)0xFF);
            }

            // Find FreeSpace if necessary
            if (GetTrackSize() > originalTrackSize) // Needs to repoint
            {
                FreeSpaceFinderDialog fsf = new FreeSpaceFinderDialog(rom, (uint)GetTrackSize());
                if (fsf.ShowDialog() != System.Windows.Forms.DialogResult.OK) return false; // Grr.

                // Change track offset. The main form will repoint it
                trackOffset = fsf.FreeSpaceOffset;

                // TODO: Overwrite old track
            }

            // Write the track
            bw.BaseStream.Seek(trackOffset, SeekOrigin.Begin);
            foreach (Command cmd in trackData)
            {
                // Write the appropriate stuff.
                if (cmd is Repeat)
                {
                    // Just write arguments.
                    Repeat rep = (Repeat)cmd;
                    for (int i = 0; i < rep.Arguments.Count; i++)
                    {
                        bw.Write(rep.Arguments[i]);
                    }
                }
                else if (cmd is Jump)
                {
                    // Write command and pointer (we calculate!)
                    Jump jmp = (Jump)cmd;
                    bw.Write(jmp.Value);
                    bw.WritePointer((uint)(jmp.RelativePointer + trackOffset));
                }
                else if (cmd is Generic)
                {
                    Generic gen = (Generic)cmd;
                    bw.Write(gen.Value);
                    for (int i = 0; i < gen.Arguments.Count; i++)
                    {
                        bw.Write(gen.Arguments[i]);
                    }
                }
                else if (cmd is xCommand) // Command 0xCD -- todo: is actually two uint16's
                {
                    xCommand x = (xCommand)cmd;
                    bw.Write(x.Value);
                    bw.Write(x.xValue);
                }
                else
                {
                    bw.Write(cmd.Value);
                }
            }

            bw.Dispose();
            return true;
        }

        private byte PeekByte(BinaryReader br)
        {
            byte b = br.ReadByte();
            br.BaseStream.Seek(-1, SeekOrigin.Current);
            return b;
        }

        private byte[] PeekBytes(BinaryReader br, int count)
        {
            byte[] b = br.ReadBytes(count);
            br.BaseStream.Seek(-count, SeekOrigin.Current);
            return b;
        }
    }
}
