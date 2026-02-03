using System.IO;
using System.Threading.Tasks;
using Moffat.EndlessOnline.SDK.Data;
using Moffat.EndlessOnline.SDK.Protocol.Pub;

namespace SOE_PubEditor.Services;

/// <summary>
/// Service for loading and saving pub files using eolib-dotnet SDK.
/// </summary>
public class PubFileService : IPubFileService
{
    public async Task<Eif> LoadItemsAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var bytes = File.ReadAllBytes(filePath);
            var reader = new EoReader(bytes);
            var eif = new Eif();
            eif.Deserialize(reader);
            return eif;
        });
    }

    public async Task<Enf> LoadNpcsAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var bytes = File.ReadAllBytes(filePath);
            var reader = new EoReader(bytes);
            var enf = new Enf();
            enf.Deserialize(reader);
            return enf;
        });
    }

    public async Task<Esf> LoadSpellsAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var bytes = File.ReadAllBytes(filePath);
            var reader = new EoReader(bytes);
            var esf = new Esf();
            esf.Deserialize(reader);
            return esf;
        });
    }

    public async Task<Ecf> LoadClassesAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var bytes = File.ReadAllBytes(filePath);
            var reader = new EoReader(bytes);
            var ecf = new Ecf();
            ecf.Deserialize(reader);
            return ecf;
        });
    }

    public async Task SaveItemsAsync(string filePath, Eif data)
    {
        await Task.Run(() =>
        {
            var writer = new EoWriter();
            data.Serialize(writer);
            File.WriteAllBytes(filePath, writer.ToByteArray());
        });
    }

    public async Task SaveNpcsAsync(string filePath, Enf data)
    {
        await Task.Run(() =>
        {
            var writer = new EoWriter();
            data.Serialize(writer);
            File.WriteAllBytes(filePath, writer.ToByteArray());
        });
    }

    public async Task SaveSpellsAsync(string filePath, Esf data)
    {
        await Task.Run(() =>
        {
            var writer = new EoWriter();
            data.Serialize(writer);
            File.WriteAllBytes(filePath, writer.ToByteArray());
        });
    }

    public async Task SaveClassesAsync(string filePath, Ecf data)
    {
        await Task.Run(() =>
        {
            var writer = new EoWriter();
            data.Serialize(writer);
            File.WriteAllBytes(filePath, writer.ToByteArray());
        });
    }
}
