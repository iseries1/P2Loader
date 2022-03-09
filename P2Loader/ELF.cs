using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2Loader
{
    class ELF
    {
        public Header ElfHdr;
        public PHeader[] PgmHdr;
        public SHeader[] SHdr;
        public int LoadBase = -1;
        public int LoadSize = 0;
        public int LoadSegments = 0;
        public ELF(byte[] b)
        {
            int p = 0;
            int n = 0;
            string name;

            ElfHdr = new Header();

            for (int i = 0; i < 4; i++)
                ElfHdr.magic[i] = b[p++];
            ElfHdr.size = b[p++];
            ElfHdr.endianess = b[p++];
            ElfHdr.id = b[p++];
            ElfHdr.os[0] = b[p++];
            ElfHdr.os[1] = b[p++];
            for (int i = 0; i < 7; i++)
                ElfHdr.zeros[i] = b[p++];
            ElfHdr.type = value(b, p, 2);
            p += 2;
            ElfHdr.machine = value(b, p, 2);
            p += 2;
            ElfHdr.version = value(b, p, 4);
            p += 4;
            if (ElfHdr.size == 1)
            {
                ElfHdr.entry = value(b, p, 4);
                p += 4;
                ElfHdr.phoff = value(b, p, 4);
                p += 4;
                ElfHdr.shoff = value(b, p, 4);
                p += 4;
            }
            else
            {
                ElfHdr.entry = value(b, p, 8);
                p += 8;
                ElfHdr.phoff = value(b, p, 8);
                p += 8;
                ElfHdr.shoff = value(b, p, 8);
                p += 8;
            }
            ElfHdr.flags = value(b, p, 4);
            p += 4;
            ElfHdr.ehsize = value(b, p, 2);
            p += 2;
            ElfHdr.phentsize = value(b, p, 2);
            p += 2;
            ElfHdr.phnum = value(b, p, 2);
            p += 2;
            ElfHdr.shentsize = value(b, p, 2);
            p += 2;
            ElfHdr.shnum = value(b, p, 2);
            p += 2;
            ElfHdr.shstrndx = value(b, p, 2);
            p += 2;
            ElfHdr.eof = p;

            p = ElfHdr.phoff;
            PgmHdr = new PHeader[ElfHdr.phnum];

            for (int i = 0; i < ElfHdr.phnum; i++)
            {
                PgmHdr[i] = new PHeader();
                if (ElfHdr.size == 1)
                {
                    PgmHdr[i].type = value(b, p, 4);
                    p += 4;
                    PgmHdr[i].offset = value(b, p, 4);
                    p += 4;
                    PgmHdr[i].vaddr = value(b, p, 4);
                    p += 4;
                    PgmHdr[i].paddr = value(b, p, 4);
                    p += 4;
                    PgmHdr[i].filesz = value(b, p, 4);
                    p += 4;
                    PgmHdr[i].memsz = value(b, p, 4);
                    p += 4;
                    PgmHdr[i].flags = value(b, p, 4);
                    p += 4;
                    PgmHdr[i].align = value(b, p, 4);
                    p += 4;
                }
                else
                {
                    PgmHdr[i].type = value(b, p, 4);
                    p += 4;
                    PgmHdr[i].flags = value(b, p, 4);
                    p += 4;
                    PgmHdr[i].offset = value(b, p, 8);
                    p += 8;
                    PgmHdr[i].vaddr = value(b, p, 8);
                    p += 8;
                    PgmHdr[i].paddr = value(b, p, 8);
                    p += 8;
                    PgmHdr[i].filesz = value(b, p, 8);
                    p += 8;
                    PgmHdr[i].memsz = value(b, p, 8);
                    p += 8;
                    PgmHdr[i].align = value(b, p, 8);
                    p += 8;
                }
                PgmHdr[i].eof = p;
                if (PgmHdr[i].type == 1)
                {
                    if (LoadBase < 0)
                        LoadBase = PgmHdr[i].paddr;
                    n = PgmHdr[i].paddr + PgmHdr[i].filesz;// + PgmHdr[i].memsz;
                    if (n > LoadSize)
                        LoadSize = n;
                }
            }

            p = ElfHdr.shoff;

            SHdr = new SHeader[ElfHdr.shnum];
            n = 0;

            for (int i = 0; i < ElfHdr.shnum; i++)
            {
                SHdr[i] = new SHeader();
                if (ElfHdr.size == 1)
                {
                    SHdr[i].nameidx = value(b, p, 4);
                    p += 4;
                    SHdr[i].type = value(b, p, 4);
                    p += 4;
                    SHdr[i].flags = value(b, p, 4);
                    p += 4;
                    SHdr[i].addr = value(b, p, 4);
                    p += 4;
                    SHdr[i].offset = value(b, p, 4);
                    p += 4;
                    SHdr[i].size = value(b, p, 4);
                    p += 4;
                    SHdr[i].link = value(b, p, 4);
                    p += 4;
                    SHdr[i].info = value(b, p, 4);
                    p += 4;
                    SHdr[i].addralign = value(b, p, 4);
                    p += 4;
                    SHdr[i].entrysize = value(b, p, 4);
                    p += 4;
                }
                else
                {
                    SHdr[i].nameidx = value(b, p, 4);
                    p += 4;
                    SHdr[i].type = value(b, p, 8);
                    p += 8;
                    SHdr[i].flags = value(b, p, 8);
                    p += 8;
                    SHdr[i].addr = value(b, p, 8);
                    p += 8;
                    SHdr[i].offset = value(b, p, 8);
                    p += 8;
                    SHdr[i].size = value(b, p, 8);
                    p += 8;
                    SHdr[i].addralign = value(b, p, 8);
                    p += 8;
                    SHdr[i].entrysize = value(b, p, 8);
                    p += 8;
                }
                SHdr[i].eof = p;

                if ((SHdr[i].type == 3) && (n == 0))
                    n = i;
                //SHdr.name = value(b, SHdr.addr + SHdr.nameidx);
            }

            for (int i = 0; i <= n; i++)
            {
                name = value(b, SHdr[n].offset + SHdr[i].nameidx);
                SHdr[i].name = name;
            }
        }

        int value(byte[] x, int s, int n)
        {
            int v = 0;
            int r = 0;
            for (int i = 0; i < n; i++)
            {
                v = v | x[s++] << r;
                r += 8;
            }
            return v;
        }

        string value(byte[] x, int s)
        {
            string v = "";
            byte[] b = new byte[10];

            for (int i = 0; i < 10; i++)
            {
                if (x[s] == 0)
                {
                    v = Encoding.UTF8.GetString(b, 0, i);
                    break;
                }
                b[i] = x[s++];
            }

            return v;
        }

        public byte[] getProgram(byte[] p)
        {
            int i;

            byte[] b = new byte[this.LoadSize];
            for (int k = 0; k < this.ElfHdr.phnum; k++)
            {
                if (this.PgmHdr[k].type != 1)
                    continue;
                i = this.PgmHdr[k].offset;
#if DEBUG
                System.Console.WriteLine(string.Format("Load Address:{0:X}, Size:{1}, Memory:{2}",
                    this.PgmHdr[k].paddr, this.PgmHdr[k].filesz, this.PgmHdr[k].memsz));
#endif
                for (int j = 0; j < this.PgmHdr[k].filesz; j++)
                    b[j + this.PgmHdr[k].paddr] = p[i++];
            }
            return b;
        }
    }
    class SHeader
    {
        public int nameidx;
        public int type;
        public int flags;
        public int addr;
        public int offset;
        public int size;
        public int link;
        public int info;
        public int addralign;
        public int entrysize;
        public int eof;
        public string name;
    }
    class PHeader
    {
        public int type;
        public int flags;
        public int offset;
        public int vaddr;
        public int paddr;
        public int filesz;
        public int memsz;
        public int align;
        public int eof;
    }
    class Header
    {
        public byte[] magic = new byte[4];
        public byte size;
        public byte endianess;
        public byte id;
        public byte[] os = new byte[2];
        public byte[] zeros = new byte[7];
        public int type;
        public int machine;
        public int version;
        public int entry;
        public int phoff;
        public int shoff;
        public int flags;
        public int ehsize;
        public int phentsize;
        public int phnum;
        public int shentsize;
        public int shnum;
        public int shstrndx;
        public int eof;

    }
}
