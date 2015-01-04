﻿using Moq;
using NUnit.Framework;
using Stylet;
using Stylet.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace StyletUnitTests
{
    public class ViewManagerTestsViewModel
    {
    }

    public class ViewManagerTestsView
    {
    }

    [TestFixture, RequiresSTA]
    public class ViewManagerTests
    {
        private interface I1 { }
        private abstract class AC1 { }
        private class C1 { }
        private Mock<IViewManagerConfig> viewManagerConfig;

        private class AccessibleViewManager : ViewManager
        {
            public AccessibleViewManager(IViewManagerConfig config) : base(config) { }

            public new UIElement CreateViewForModel(object model)
            {
                return base.CreateViewForModel(model);
            }

            public new void BindViewToModel(UIElement view, object viewModel)
            {
                base.BindViewToModel(view, viewModel);
            }
        }

        private class CreatingAndBindingViewManager : ViewManager
        {
            public UIElement View;
            public object RequestedModel;

            public CreatingAndBindingViewManager(IViewManagerConfig config) : base(config) { }

            protected override UIElement CreateViewForModel(object model)
            {
                this.RequestedModel = model;
                return this.View;
            }

            public UIElement BindViewToModelView;
            public object BindViewtoModelViewModel;
            protected override void BindViewToModel(UIElement view, object viewModel)
            {
                this.BindViewToModelView = view;
                this.BindViewtoModelViewModel = viewModel;
            }
        }

        private class LocatingViewManager : ViewManager
        {
            public LocatingViewManager(IViewManagerConfig config) : base(config) { }

            public Type LocatedViewType;
            protected override Type LocateViewForModel(Type modelType)
            {
 	             return this.LocatedViewType;
            }
        }


        private class TestView : UIElement
        {
            public bool InitializeComponentCalled;
            public void InitializeComponent()
            {
                this.InitializeComponentCalled = true;
            }
        }

        private class MyViewManager : ViewManager
        {
            public MyViewManager(IViewManagerConfig config) : base(config) { }

            public new Type LocateViewForModel(Type modelType)
            {
                return base.LocateViewForModel(modelType);
            }
        }

        private MyViewManager viewManager;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            Execute.TestExecuteSynchronously = true;
        }

        [SetUp]
        public void SetUp()
        {
            this.viewManagerConfig = new Mock<IViewManagerConfig>();
            this.viewManager = new MyViewManager(this.viewManagerConfig.Object);
        }

        [Test]
        public void OnModelChangedDoesNothingIfNoChange()
        {
            var val = new object();
            this.viewManager.OnModelChanged(null, val, val);
        }

        [Test]
        public void OnModelChangedSetsNullIfNewValueNull()
        {
            var target = new ContentControl();
            this.viewManager.OnModelChanged(target, 5, null);
            Assert.Null(target.Content);
        }

        [Test]
        public void OnModelChangedUsesViewIfAlreadySet()
        {
            var target = new ContentControl();
            var model = new Mock<IScreen>();
            var view = new UIElement();

            model.Setup(x => x.View).Returns(view);
            this.viewManager.OnModelChanged(target, null, model.Object);

            Assert.AreEqual(view, target.Content);
        }

        [Test]
        public void OnModelChangedCreatesAndBindsView()
        {
            var target = new ContentControl();
            var model = new object();
            var view = new UIElement();
            var viewManager = new CreatingAndBindingViewManager(this.viewManagerConfig.Object);

            viewManager.View = view;

            viewManager.OnModelChanged(target, null, model);

            Assert.AreEqual(viewManager.RequestedModel, model);
            Assert.AreEqual(viewManager.BindViewToModelView, view);
            Assert.AreEqual(viewManager.BindViewtoModelViewModel, model);
            Assert.AreEqual(view, target.Content);
        }

        [Test]
        public void LocateViewForModelThrowsIfViewNotFound()
        {
            var config = new Mock<IViewManagerConfig>();
            config.Setup(x => x.GetInstance(typeof(C1))).Returns(null);
            config.SetupGet(x => x.Assemblies).Returns(new List<Assembly>());

            var viewManager = new MyViewManager(config.Object);
            Assert.Throws<StyletViewLocationException>(() => viewManager.LocateViewForModel(typeof(C1)));
        }

        [Test]
        public void LocateViewForModelFindsViewForModel()
        {
            var config = new Mock<IViewManagerConfig>();
            config.SetupGet(x => x.Assemblies).Returns(new List<Assembly>() { Assembly.GetExecutingAssembly() });
            var viewManager = new MyViewManager(config.Object);
            Execute.TestExecuteSynchronously = true;
            var viewType = viewManager.LocateViewForModel(typeof(ViewManagerTestsViewModel));
            Assert.AreEqual(typeof(ViewManagerTestsView), viewType);
        }

        [Test]
        public void CreateViewForModelThrowsIfViewIsNotConcreteUIElement()
        {
            var viewManager = new LocatingViewManager(this.viewManagerConfig.Object);

            viewManager.LocatedViewType = typeof(I1);
            Assert.Throws<StyletViewLocationException>(() => viewManager.CreateAndBindViewForModel(new object()));

            viewManager.LocatedViewType = typeof(AC1);
            Assert.Throws<StyletViewLocationException>(() => viewManager.CreateAndBindViewForModel(new object()));

            viewManager.LocatedViewType = typeof(C1);
            Assert.Throws<StyletViewLocationException>(() => viewManager.CreateAndBindViewForModel(new object()));
        }

        [Test]
        public void CreateViewForModelCallsFetchesViewAndCallsInitializeComponent()
        {
            var view = new TestView();
            var config = new Mock<IViewManagerConfig>();
            config.Setup(x => x.GetInstance(typeof(TestView))).Returns(view);
            var viewManager = new LocatingViewManager(config.Object);
            viewManager.LocatedViewType = typeof(TestView);

            var returnedView = viewManager.CreateAndBindViewForModel(new object());

            Assert.True(view.InitializeComponentCalled);
            Assert.AreEqual(view, returnedView);
        }

        [Test]
        public void CreateViewForModelDoesNotComplainIfNoInitializeComponentMethod()
        {
            var view = new UIElement();
            var config = new Mock<IViewManagerConfig>();
            config.Setup(x => x.GetInstance(typeof(UIElement))).Returns(view);
            var viewManager = new LocatingViewManager(config.Object);
            viewManager.LocatedViewType = typeof(UIElement);

            var returnedView = viewManager.CreateAndBindViewForModel(new object());

            Assert.AreEqual(view, returnedView);
        }

        [Test]
        public void BindViewToModelSetsActionTarget()
        {
            var view = new UIElement();
            var model = new object();
            var viewManager = new AccessibleViewManager(this.viewManagerConfig.Object);
            viewManager.BindViewToModel(view, model);

            Assert.AreEqual(model, View.GetActionTarget(view));
        }

        [Test]
        public void BindViewToModelSetsDataContext()
        {
            var view = new FrameworkElement();
            var model = new object();
            var viewManager = new AccessibleViewManager(this.viewManagerConfig.Object);
            viewManager.BindViewToModel(view, model);

            Assert.AreEqual(model, view.DataContext);
        }

        [Test]
        public void BindViewToModelAttachesView()
        {
            var view = new UIElement();
            var model = new Mock<IViewAware>();
            var viewManager = new AccessibleViewManager(this.viewManagerConfig.Object);
            viewManager.BindViewToModel(view, model.Object);

            model.Verify(x => x.AttachView(view));
        }
    }
}