using System; using System.IO; using System.Linq; using System.IO.Compression; using System.Collections.Generic;
namespace Il2CppDumper {
public static class MainForm { public static Action<string> Sink; public static void Log(string s) => Sink?.Invoke(s); public static void Log(string s, object x) => Log(string.Format(s,x)); }
internal static class Resource1 { public static byte[] Il2CppDummyDll { get { using var s=typeof(Resource1).Assembly.GetManifestResourceStream("DummyTemplate"); using var m=new MemoryStream(); s.CopyTo(m); return m.ToArray(); } } }
public class Engine {
Config config; void Log(string s)=>MainForm.Log(s);
private bool Init(byte[] il2cppBytes, byte[] metadataBytes, int machIndex, ulong dumpBase, string code, string meta, out Metadata metadata, out Il2Cpp il2Cpp) {
            Log("Initializing metadata...");
            
            metadata = new Metadata(new MemoryStream(metadataBytes));
            Log($"Metadata Version: {metadata.Version}");
            var unityRange = UnityVersionMap.GetUnityVersionRange(metadata.Version);
            Log($"Detected Unity Version: {unityRange}");
            if (metadata.Version >= 38)
            {
                Log("Unity 6 metadata detected - using variable-width index mode");
            }
            Log("Initializing il2cpp file...");
            
            var il2cppMagic = BitConverter.ToUInt32(il2cppBytes, 0);
            var il2CppMemory = new MemoryStream(il2cppBytes);
            switch (il2cppMagic)
            {
                default:
                    throw new NotSupportedException("ERROR: il2cpp file not supported.");
                case 0x6D736100:
                    var web = new WebAssembly(il2CppMemory);
                    il2Cpp = web.CreateMemory();
                    break;
                case 0x304F534E:
                    var nso = new NSO(il2CppMemory);
                    il2Cpp = nso.UnCompress();
                    break;
                case 0x905A4D: //PE
                    il2Cpp = new PE(il2CppMemory);
                    break;
                case 0x464c457f: //ELF
                    if (il2cppBytes[4] == 2) //ELF64
                    {
                        il2Cpp = new Elf64(il2CppMemory);
                    }
                    else
                    {
                        il2Cpp = new Elf(il2CppMemory);
                    }
                    break;
                case 0xCAFEBABE: //FAT Mach-O
                case 0xBEBAFECA:
                    var machofat = new MachoFat(new MemoryStream(il2cppBytes));
                    for (var i = 0; i < machofat.fats.Length; i++)
                    {
                        var fat = machofat.fats[i];
                        //Console.Write(fat.magic == 0xFEEDFACF ? $"{i + 1}.64bit " : $"{i + 1}.32bit ");
                    }
                    var index = machIndex;
                    var magic = machofat.fats[index].magic;
                    il2cppBytes = machofat.GetMacho(index);
                    il2CppMemory = new MemoryStream(il2cppBytes);
                    if (magic == 0xFEEDFACF)
                        goto case 0xFEEDFACF;
                    else
                        goto case 0xFEEDFACE;
                case 0xFEEDFACF: // 64bit Mach-O
                    il2Cpp = new Macho64(il2CppMemory);
                    break;
                case 0xFEEDFACE: // 32bit Mach-O
                    il2Cpp = new Macho(il2CppMemory);
                    break;
            }
            var version = config.ForceIl2CppVersion ? config.ForceVersion : metadata.Version;
            il2Cpp.SetProperties(version, metadata.metadataUsagesCount, metadata);
            Log($"Il2Cpp Version: {il2Cpp.Version}");
            if (config.ForceDump || il2Cpp.CheckDump())
            {
                if (il2Cpp is ElfBase elf)
                {
                    if (dumpBase != 0) { il2Cpp.ImageBase = dumpBase; il2Cpp.IsDumped = true; if (!config.NoRedirectedPointer) elf.Reload(); }
                    else il2Cpp.IsDumped = true;
                }
            }

            Log("Searching...");
            try
            {
                var flag = il2Cpp.PlusSearch(metadata.methodDefs.Count(x => x.methodIndex >= 0), metadata.typeDefs.Length, metadata.imageDefs.Length);
                if (!flag)
                {
                    flag = il2Cpp.Search();
                }
                if (!flag)
                {
                    flag = il2Cpp.SymbolSearch();
                }
                if (!flag)
                {
                    Log("ERROR: Can't use auto mode to process file, trying manual mode...");
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(meta))
                    {
                        Log("CodeRegistration or MetadataRegistration is empty");
                        return false;
                    }
                    var codeRegistration = Convert.ToUInt64(code, 16);
                    var metadataRegistration = Convert.ToUInt64(meta, 16);
                    il2Cpp.Init(codeRegistration, metadataRegistration);
                }
                if (il2Cpp.Version >= 27 && il2Cpp.IsDumped)
                {
                    var typeDef = metadata.typeDefs[0];
                    var il2CppType = il2Cpp.types[typeDef.byvalTypeIndex];
                    var typeDefinitionsOffset = metadata.Version >= 38
                        ? (ulong)metadata.header.typeDefinitions.offset
                        : metadata.header.typeDefinitionsOffset;
                    metadata.ImageBase = il2CppType.data.typeHandle - typeDefinitionsOffset;
                }
            }
            catch (Exception ex)
            {
                Log("An error occurred while processing.");
                Log(ex.ToString());
                return false;
            }
            return true;
        }


public byte[] Run(byte[] binary, byte[] metadataBytes, Config options, bool dump, bool fast, int machIndex, ulong dumpBase, string code, string meta) {
config=options; var dir="/tmp/"+Guid.NewGuid().ToString("N"); Directory.CreateDirectory(dir); var old=Directory.GetCurrentDirectory();
try { if(!Init(binary,metadataBytes,machIndex,dumpBase,code,meta,out var metadata,out var il2cpp)) throw new InvalidOperationException("Initialization failed"); var ex=new Il2CppExecutor(metadata,il2cpp); var output=dir+"/"; if(dump) new Il2CppDecompiler(ex).Decompile(config,output); if(config.GenerateStruct) new StructGenerator(ex).WriteScript(output,fast,1); if(config.GenerateDummyDll) DummyAssemblyExporter.Export(ex,output,config.DummyDllAddToken,1); Directory.SetCurrentDirectory(old); using var m=new MemoryStream(); using(var z=new ZipArchive(m,ZipArchiveMode.Create,true)) foreach(var f in Directory.GetFiles(dir,"*",SearchOption.AllDirectories)) z.CreateEntryFromFile(f,Path.GetRelativePath(dir,f)); return m.ToArray(); } finally { Directory.SetCurrentDirectory(old); Directory.Delete(dir,true); }
} } }
