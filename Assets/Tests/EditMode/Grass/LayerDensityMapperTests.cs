using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    public class LayerDensityMapperTests
    {
        // Gerçek terrain alphamap sırası (tek RGBA doku): R=Grass_A, G=Grass_Dry, B=Snow, A=Muddy.
        static readonly string[] Names = { "Grass_A", "Grass_Dry", "Snow", "Muddy" };

        static LayerDensityRule Rule(int index, string name, float density, float height = 1f)
            => new LayerDensityRule(index, name, density, new Color(0.1f, 0.2f, 0.1f), new Color(0.6f, 0.8f, 0.3f), height);

        static List<LayerDensityRule> DefaultRules() => new List<LayerDensityRule>
        {
            Rule(0, "Grass_A", 1f, 1f),
            Rule(1, "Grass_Dry", 0.6f, 0.8f),
            Rule(2, "Snow", 0f, 1f),
            Rule(3, "Muddy", 0.3f, 0.5f),
        };

        static bool Create(IReadOnlyList<string> names, IReadOnlyList<LayerDensityRule> rules,
                           out LayerDensityMapper mapper, out ValidationReport report)
        {
            bool ok = LayerDensityMapper.TryCreate(names, rules, out mapper, out report);
            Assert.AreEqual(ok, report.IsValid);
            Assert.AreEqual(ok, mapper != null, "Mapper yalnızca geçerli veride üretilmeli");
            return ok;
        }

        [Test]
        public void FourLayers_MapToRgbaChannels()
        {
            Assert.IsTrue(Create(Names, DefaultRules(), out LayerDensityMapper m, out ValidationReport report), report.ToString());

            Assert.AreEqual(new Vector4(1f, 0.6f, 0f, 0.3f), m.DensityMultipliers);
            Assert.AreEqual(new Vector4(1f, 0.8f, 1f, 0.5f), m.HeightMultipliers);
            Assert.AreEqual(4, m.RootTints.Length);
            Assert.AreEqual(new Vector4(0.1f, 0.2f, 0.1f, 1f), m.RootTints[1]);
            Assert.AreEqual(new Vector4(0.6f, 0.8f, 0.3f, 1f), m.TipTints[3]);
            Assert.AreEqual(0, report.Warnings.Count);
        }

        [Test]
        public void EvaluateDensity_BlendsBySplatWeights()
        {
            Assert.IsTrue(Create(Names, DefaultRules(), out LayerDensityMapper m, out _));
            Assert.AreEqual(0.8f, m.EvaluateDensity(new Vector4(0.5f, 0.5f, 0f, 0f)), 1e-5f);
            Assert.AreEqual(0f, m.EvaluateDensity(new Vector4(0f, 0f, 1f, 0f)), 1e-5f); // tam kar: çim yok
        }

        [Test]
        public void FewerThanFourLayers_LeavesUnusedChannelsEmpty()
        {
            var names = new[] { "Grass_A", "Grass_Dry" };
            var rules = new List<LayerDensityRule> { Rule(0, "Grass_A", 1f), Rule(1, "Grass_Dry", 0.5f) };
            Assert.IsTrue(Create(names, rules, out LayerDensityMapper m, out _));
            Assert.AreEqual(new Vector4(1f, 0.5f, 0f, 0f), m.DensityMultipliers);
        }

        [Test]
        public void MoreThanFourLayers_RulesOnFirstFour_WarnsAboutIgnoredLayers()
        {
            var names = new[] { "Grass_A", "Grass_Dry", "Snow", "Muddy", "Rock" };
            Assert.IsTrue(Create(names, DefaultRules(), out _, out ValidationReport report));
            Assert.AreEqual(1, report.Warnings.Count);
        }

        [Test]
        public void RuleOnFifthLayer_IsError_BecauseItLivesInSecondAlphamap()
        {
            var names = new[] { "Grass_A", "Grass_Dry", "Snow", "Muddy", "Rock" };
            var rules = DefaultRules();
            rules.Add(Rule(4, "Rock", 1f));
            Assert.IsFalse(Create(names, rules, out _, out ValidationReport report));
            Assert.IsTrue(report.Errors.Count >= 1);
        }

        [Test]
        public void LayerIndexOutOfRange_IsError()
        {
            var rules = DefaultRules();
            rules.Add(Rule(9, null, 1f));
            Assert.IsFalse(Create(Names, rules, out _, out _));

            Assert.IsFalse(Create(Names, new List<LayerDensityRule> { Rule(-1, null, 1f) }, out _, out _));
        }

        [Test]
        public void DuplicateRuleForSameLayer_IsError()
        {
            var rules = DefaultRules();
            rules.Add(Rule(0, "Grass_A", 0.5f));
            Assert.IsFalse(Create(Names, rules, out _, out _));
        }

        [Test]
        public void NameMismatch_IsWarning_NotError()
        {
            var rules = DefaultRules();
            rules[2] = Rule(2, "Rock", 0f); // terrain'de 2. layer 'Snow'
            Assert.IsTrue(Create(Names, rules, out _, out ValidationReport report));
            Assert.AreEqual(1, report.Warnings.Count);
        }

        [Test]
        public void NullExpectedName_SkipsNameCheck()
        {
            var rules = new List<LayerDensityRule> { Rule(0, null, 1f), Rule(1, null, 1f), Rule(2, null, 0f), Rule(3, null, 1f) };
            Assert.IsTrue(Create(Names, rules, out _, out ValidationReport report));
            Assert.AreEqual(0, report.Warnings.Count);
        }

        [Test]
        public void LayerWithoutRule_WarnsAndHasZeroDensity()
        {
            var rules = new List<LayerDensityRule> { Rule(0, "Grass_A", 1f), Rule(1, "Grass_Dry", 0.6f) };
            Assert.IsTrue(Create(Names, rules, out LayerDensityMapper m, out ValidationReport report));
            Assert.AreEqual(2, report.Warnings.Count); // Snow ve Muddy için
            Assert.AreEqual(0f, m.DensityMultipliers.z);
            Assert.AreEqual(0f, m.DensityMultipliers.w);
        }

        [Test]
        public void EmptyOrNullInputs_AreErrors()
        {
            Assert.IsFalse(Create(null, DefaultRules(), out _, out _));
            Assert.IsFalse(Create(new string[0], DefaultRules(), out _, out _));
            Assert.IsFalse(Create(Names, null, out _, out _));
            Assert.IsFalse(Create(Names, new List<LayerDensityRule>(), out _, out _));
        }

        [TestCase(float.NaN, 1f)]
        [TestCase(-0.1f, 1f)]
        [TestCase(float.PositiveInfinity, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(1f, -1f)]
        [TestCase(1f, float.NaN)]
        public void InvalidNumbers_AreErrors(float density, float height)
        {
            var rules = DefaultRules();
            rules[0] = Rule(0, "Grass_A", density, height);
            Assert.IsFalse(Create(Names, rules, out _, out ValidationReport report));
            Assert.IsNotEmpty(report.ToString());
        }

        [Test]
        public void NaNTint_IsError()
        {
            var rules = DefaultRules();
            rules[0] = new LayerDensityRule(0, "Grass_A", 1f, new Color(float.NaN, 0f, 0f), Color.green, 1f);
            Assert.IsFalse(Create(Names, rules, out _, out _));
        }

        static List<LayerDensityRule> RulesWithColors(Color root, Color tip, float density = 1f)
        {
            var rules = DefaultRules();
            rules[0] = new LayerDensityRule(0, "Grass_A", density, root, tip, 1f);
            return rules;
        }

        [Test]
        public void LayerTints_RgbIsTip_ShadeIsLuminanceRatio()
        {
            Assert.IsTrue(Create(Names, DefaultRules(), out LayerDensityMapper m, out _));
            Assert.AreEqual(4, m.LayerTints.Length);

            // Rec.709: root (0.1,0.2,0.1) / tip (0.6,0.8,0.3).
            float rootLum = 0.2126f * 0.1f + 0.7152f * 0.2f + 0.0722f * 0.1f;
            float tipLum = 0.2126f * 0.6f + 0.7152f * 0.8f + 0.0722f * 0.3f;
            Vector4 t = m.LayerTints[0];
            Assert.AreEqual(0.6f, t.x, 1e-5f);
            Assert.AreEqual(0.8f, t.y, 1e-5f);
            Assert.AreEqual(0.3f, t.z, 1e-5f);
            Assert.AreEqual(rootLum / tipLum, t.w, 1e-5f);
        }

        [Test]
        public void LayerTints_SameRootAndTip_ShadeIsOne()
        {
            var snow = new Color(0.9f, 0.92f, 0.95f);
            Assert.IsTrue(Create(Names, RulesWithColors(snow, snow), out LayerDensityMapper m, out ValidationReport report));
            Assert.AreEqual(1f, m.LayerTints[0].w, 1e-5f);
            Assert.AreEqual(0, report.Warnings.Count, report.ToString());
        }

        [Test]
        public void LayerTints_RootBrighterThanTip_ClampsShadeAndWarns()
        {
            Assert.IsTrue(Create(Names, RulesWithColors(Color.white, new Color(0.3f, 0.3f, 0.3f)),
                                 out LayerDensityMapper m, out ValidationReport report));
            Assert.AreEqual(1f, m.LayerTints[0].w, 1e-6f);
            Assert.IsTrue(report.Warnings.Count >= 1 && report.ToString().Contains("parlak"), report.ToString());
        }

        [Test]
        public void LayerTints_RootBrighterOnZeroDensityLayer_NoWarning()
        {
            Assert.IsTrue(Create(Names, RulesWithColors(Color.white, new Color(0.3f, 0.3f, 0.3f), density: 0f),
                                 out _, out ValidationReport report));
            Assert.AreEqual(0, report.Warnings.Count, report.ToString());
        }

        [Test]
        public void LayerTints_DifferentHueRoot_WarnsButStaysValid()
        {
            // Aynı parlaklık oranı ama kök mavi, uç yeşil: ton yalnızca yaklaşık temsil edilir.
            Assert.IsTrue(Create(Names, RulesWithColors(new Color(0f, 0f, 0.5f), new Color(0.1f, 0.8f, 0.1f)),
                                 out LayerDensityMapper m, out ValidationReport report));
            Assert.Greater(m.LayerTints[0].w, 0f);
            Assert.Less(m.LayerTints[0].w, 1f);
            Assert.AreEqual(1, report.Warnings.Count, report.ToString());
        }

        [Test]
        public void LayerTints_BlackTip_DoesNotDivideByZero()
        {
            Assert.IsTrue(Create(Names, RulesWithColors(Color.black, Color.black), out LayerDensityMapper m, out _));
            Assert.AreEqual(1f, m.LayerTints[0].w, 1e-6f);
        }
    }
}
