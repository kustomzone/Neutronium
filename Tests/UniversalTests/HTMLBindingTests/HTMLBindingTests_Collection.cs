﻿using FluentAssertions;
using Neutronium.Core;
using Neutronium.Core.Binding.GlueObject;
using Neutronium.Core.WebBrowserEngine.JavascriptObject;
using Neutronium.Example.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests.Infra.IntegratedContextTesterHelper.Windowless;
using Tests.Infra.WebBrowserEngineTesterHelper.HtmlContext;
using Tests.Universal.HTMLBindingTests.Helper;
using Xunit;

namespace Tests.Universal.HTMLBindingTests
{
    public abstract partial class HtmlBindingTests
    {
        private static void Checkstring(IJavascriptObject coll, IEnumerable<string> iskill)
        {
            var javaCollection = Enumerable.Range(0, coll.GetArrayLength()).Select(i => coll.GetValue(i).GetStringValue());
            javaCollection.Should().Equal(iskill);
        }

        private static void CheckDecimalCollection(IJavascriptObject coll, IList<decimal> iskill)
        {
            coll.GetArrayLength().Should().Be(iskill.Count);

            for (var i = 0; i < iskill.Count; i++)
            {
                var c = (decimal)coll.GetValue(i).GetDoubleValue();
                c.Should().Be(iskill[i]);
            }
        }

        [Fact]
        public async Task TwoWay_Maps_Collection()
        {
            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var col = GetCollectionAttribute(js, "Skills");
                    col.Should().NotBeNull();
                    col.GetArrayLength().Should().Be(2);

                    Check(col, _DataContext.Skills);

                    DoSafeUI(() =>
                    {
                        _DataContext.Skills.Add(new Skill() { Name = "C++", Type = "Info" });
                    });

                    await Task.Delay(1000);
                    col = GetCollectionAttribute(js, "Skills");
                    col.Should().NotBeNull();
                    Check(col, _DataContext.Skills);

                    DoSafeUI(() =>
                    {
                        _DataContext.Skills.Insert(0, new Skill() { Name = "C#", Type = "Info" });
                    });
                    await Task.Delay(100);
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    col.Should().NotBeNull();
                    Check(col, _DataContext.Skills);

                    DoSafeUI(() =>
                    {
                        _DataContext.Skills.RemoveAt(1);
                    });
                    await Task.Delay(100);
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    col.Should().NotBeNull();
                    Check(col, _DataContext.Skills);

                    DoSafeUI(() =>
                    {
                        _DataContext.Skills[0] = new Skill() { Name = "HTML", Type = "Info" };
                    });
                    await Task.Delay(100);
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    col.Should().NotBeNull();
                    Check(col, _DataContext.Skills);

                    DoSafeUI(() =>
                    {
                        _DataContext.Skills[0] = new Skill() { Name = "HTML5", Type = "Info" };
                        _DataContext.Skills.Insert(0, new Skill() { Name = "HTML5", Type = "Info" });
                    });
                    await Task.Delay(100);
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    col.Should().NotBeNull();
                    Check(col, _DataContext.Skills);

                    DoSafeUI(() =>
                    {
                        _DataContext.Skills.Clear();
                    });
                    await Task.Delay(100);
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    col.Should().NotBeNull();
                    Check(col, _DataContext.Skills);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Collection_Updates_Grouped_Changes()
        {
            _DataContext.Skills.Add(new Skill() { Name = "C++", Type = "Info" });
            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    DoSafeUI(() =>
                    {
                        var skills = _DataContext.Skills;
                        skills.Add(new Skill() { Name = "C++", Type = "Info" });
                        skills.RemoveAt(2);
                        skills.RemoveAt(0);
                        skills[0] = new Skill() { Name = "Vue.js", Type = "Info" };
                        skills.Add(new Skill() { Name = "C++", Type = "Info" });
                    });

                    await Task.Delay(1000);
                    var col = GetCollectionAttribute(js, "Skills");
                    col.Should().NotBeNull();
                    Check(col, _DataContext.Skills);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Collection_Updates_CSharp_From_JS_Update()
        {
            var test = new TestInContextAsync()
            {
                Path = TestContext.Simple,
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var root = (mb as HtmlBinding).JsBrideRootObject as JsGenericObject;
                    var js = mb.JsRootObject;

                    var col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    col.GetArrayLength().Should().Be(2);

                    Check(col, _DataContext.Skills);

                    var coll = GetAttribute(js, "Skills");
                    Call(coll, "push", (root.GetAttribute("Skills") as JsArray).Items[0].GetJsSessionValue());

                    await Task.Delay(5000);

                    DoSafeUI(() =>
                    {
                        _DataContext.Skills.Should().HaveCount(3);
                        _DataContext.Skills[2].Should().Be(_DataContext.Skills[0]);
                    });

                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    Check(col, _DataContext.Skills);

                    Call(coll, "pop");

                    await Task.Delay(100);
                    DoSafeUI(() =>
                    {
                        _DataContext.Skills.Should().HaveCount(2);
                    });
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    col.Should().NotBeNull();
                    Check(col, _DataContext.Skills);

                    Call(coll, "shift");

                    await Task.Delay(100);
                    DoSafeUI(() =>
                    {
                        _DataContext.Skills.Should().HaveCount(1);
                    });
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    Check(col, _DataContext.Skills);


                    Call(coll, "unshift",
                          (root.GetAttribute("Skills") as JsArray).Items[0].GetJsSessionValue());

                    await Task.Delay(150);
                    DoSafeUI(() =>
                    {
                        _DataContext.Skills.Should().HaveCount(2);
                    });
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    Check(col, _DataContext.Skills);

                    DoSafeUI(() =>
                    {
                        _DataContext.Skills.Add(new Skill() { Type = "Langage", Name = "French" });
                    });
                    await Task.Delay(150);
                    _DataContext.Skills.Should().HaveCount(3);
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    col.Should().NotBeNull();
                    Check(col, _DataContext.Skills);

                    Call(coll, "reverse");

                    await Task.Delay(150);
                    DoSafeUI(() =>
                    {
                        _DataContext.Skills.Should().HaveCount(3);
                    });
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    Check(col, _DataContext.Skills);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Maps_String_Collection()
        {
            var datacontext = new VmWithList<string>();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var col = GetSafe(() => GetCollectionAttribute(js, "List"));
                    col.GetArrayLength().Should().Be(0);
                    await Task.Delay(200);
                    Checkstring(col, datacontext.List);

                    DoSafeUI(() =>
                    {
                        datacontext.List.Add("titi");
                    });
                    await Task.Delay(200);
                    col = GetSafe(() => GetCollectionAttribute(js, "List"));
                    Checkstring(col, datacontext.List);

                    DoSafeUI(() =>
                    {
                        datacontext.List.Add("kiki");
                        datacontext.List.Add("toto");
                    });
                    await Task.Delay(100);
                    col = GetSafe(() => GetCollectionAttribute(js, "List"));
                    Checkstring(col, datacontext.List);

                    DoSafeUI(() =>
                    {
                        datacontext.List.Move(0, 2);
                    });
                    await Task.Delay(100);
                    col = GetSafe(() => GetCollectionAttribute(js, "List"));
                    Checkstring(col, datacontext.List);

                    DoSafeUI(() =>
                    {
                        datacontext.List.Move(2, 1);
                    });
                    await Task.Delay(100);
                    col = GetSafe(() => GetCollectionAttribute(js, "List"));
                    Checkstring(col, datacontext.List);

                    var comp = new List<string>(datacontext.List) { "newvalue" };

                    col = GetSafe(() => GetCollectionAttribute(js, "List"));
                    var chcol = GetAttribute(js, "List");
                    Call(chcol, "push", _WebView.Factory.CreateString("newvalue"));

                    await Task.Delay(350);

                    col = GetSafe(() => GetCollectionAttribute(js, "List"));

                    datacontext.List.Should().Equal(comp);
                    Checkstring(col, datacontext.List);

                    DoSafeUI(() =>
                    {
                        datacontext.List.Clear();
                    });
                    await Task.Delay(100);
                    col = GetSafe(() => GetCollectionAttribute(js, "List"));

                    Checkstring(col, datacontext.List);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Updates_Collection()
        {
            var datacontext = new ChangingCollectionViewModel();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var col = GetSafe(() => GetCollectionAttribute(js, "Items"));
                    col.GetArrayLength().Should().NotBe(0);

                    DoSafeUI(() => datacontext.Replace.Execute(null));

                    datacontext.Items.Should().BeEmpty();

                    await Task.Delay(300);
                    col = GetSafe(() => GetCollectionAttribute(js, "Items"));
                    col.GetArrayLength().Should().Be(0);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Maps_None_Generic_List()
        {
            var datacontext = new VmWithList();
            datacontext.List.Add(888);

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var col = GetSafe(() => GetCollectionAttribute(js, "List"));
                    col.GetArrayLength().Should().Be(1);

                    var res = GetAttribute(js, "List");
                    Call(res, "push", _WebView.Factory.CreateString("newvalue"));

                    col = GetSafe(() => GetCollectionAttribute(js, "List"));
                    col.GetArrayLength().Should().Be(2);

                    await Task.Delay(350);

                    datacontext.List.Should().HaveCount(2);
                    datacontext.List[1].Should().Be("newvalue");
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Maps_Decimal_Collection()
        {
            var datacontext = new VmWithList<decimal>();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var col = GetSafe(() => GetCollectionAttribute(js, "List"));
                    col.GetArrayLength().Should().Be(0);

                    CheckDecimalCollection(col, datacontext.List);

                    DoSafeUI(() =>
                    {
                        datacontext.List.Add(3);
                    });

                    await Task.Delay(150);
                    col = GetSafe(() => GetCollectionAttribute(js, "List"));

                    CheckDecimalCollection(col, datacontext.List);

                    DoSafeUI(() =>
                    {
                        datacontext.List.Add(10.5m);
                        datacontext.List.Add(126);
                    });

                    await Task.Delay(150);
                    col = GetSafe(() => GetCollectionAttribute(js, "List"));

                    CheckDecimalCollection(col, datacontext.List);

                    await Task.Delay(100);
                    col = GetSafe(() => GetCollectionAttribute(js, "List"));

                    CheckDecimalCollection(col, datacontext.List);

                    var comp = new List<decimal>(datacontext.List) { 0.55m };

                    var res = GetAttribute(js, "List");
                    Call(res, "push", _WebView.Factory.CreateDouble(0.55));

                    await Task.Delay(500);

                    col = GetSafe(() => GetCollectionAttribute(js, "List"));

                    comp.Should().Equal(datacontext.List);
                    CheckDecimalCollection(col, datacontext.List);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Survives_Collection_Update_From_Js_With_Wrong_Type()
        {
            var test = new TestInContextAsync()
            {
                Path = TestContext.Simple,
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var root = (mb as HtmlBinding).JsBrideRootObject as JsGenericObject;
                    var js = mb.JsRootObject;

                    var col = GetCollectionAttribute(js, "Skills");
                    col.GetArrayLength().Should().Be(2);

                    Check(col, _DataContext.Skills);

                    var coll = GetAttribute(js, "Skills");
                    Call(coll, "push", _WebView.Factory.CreateString("Whatever"));

                    await Task.Delay(150);
                    _DataContext.Skills.Should().HaveCount(2);
                }
            };

            await RunAsync(test);
        }
    }
}
