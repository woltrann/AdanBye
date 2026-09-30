using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace AdanBye.Grass.Tests
{
    /// <summary>GrassSGInteraction.hlsl ile GrassInteractionContract'ın (dizi boyutu, global adlar) kaymadığını doğrular.</summary>
    public class GrassInteractionHlslParityTests
    {
        const string HlslPath = "Assets/Shaders/Grass/GrassSGInteraction.hlsl";

        string _text;

        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(File.Exists(HlslPath), HlslPath);
            _text = File.ReadAllText(HlslPath);
        }

        [Test]
        public void MaxInteractors_MatchesContract()
        {
            Match m = Regex.Match(_text, @"#define\s+GRASS_MAX_INTERACTORS\s+(\d+)");
            Assert.IsTrue(m.Success, "GRASS_MAX_INTERACTORS tanımı bulunamadı.");
            Assert.AreEqual(GrassInteractionContract.MaxInteractors, int.Parse(m.Groups[1].Value));
        }

        [Test]
        public void GlobalDeclarations_MatchContractNames()
        {
            StringAssert.IsMatch(@"float\s+" + GrassInteractionContract.CountName + @"\s*;", _text);
            StringAssert.IsMatch(@"float4\s+" + GrassInteractionContract.PosRadiusName + @"\s*\[\s*GRASS_MAX_INTERACTORS\s*\]\s*;", _text);
            StringAssert.IsMatch(@"float4\s+" + GrassInteractionContract.ParamsName + @"\s*\[\s*GRASS_MAX_INTERACTORS\s*\]\s*;", _text);
        }

        [Test]
        public void EntryPoint_HasExpectedSignature()
        {
            StringAssert.IsMatch(@"void\s+GrassInteraction_float\s*\(\s*float3\s+RootWS\s*,\s*out\s+float3\s+PushWS\s*,\s*out\s+float\s+Amount\s*\)", _text);
        }
    }
}
