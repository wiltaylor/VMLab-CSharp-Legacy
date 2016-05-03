using System.Collections;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Model;

namespace VMLab.Test.Model
{
    [TestClass]
    public class IdempotentActionManagerTests
    {
        [TestMethod]
        public void CallingAddOnTestManagerWillAddItToCollection()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");

            testmanager.AddAction(action.Object);
            Assert.IsTrue(testmanager.GetActions().Any(t => t.Name == "MyAction"));
        }

        [TestMethod]
        [ExpectedException(typeof(IdempotentActionAlreadyExists))]
        public void CallingAddonTestManagerWillThrowIfIdempotentActionAlreadyExists()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            var action2 = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action2.Setup(a => a.Name).Returns("MyAction");

            testmanager.AddAction(action.Object);
            testmanager.AddAction(action2.Object);    
        }

        [TestMethod]
        public void CallingRemoveOnTestManagerWillRemoveFromCollection()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");

            testmanager.AddAction(action.Object);
            testmanager.RemoveAction("MyAction");
            Assert.IsFalse(testmanager.GetActions().Any(t => t.Name == "MyAction"));
        }

        [TestMethod]
        [ExpectedException(typeof(IdempotentActionPropertyException))]
        public void CallingTestActionWillThrowIfRequiredPropertyIsNotPassed()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action.Setup(a => a.RequiredProperties).Returns(new[] {"MyReqProp"});
            testmanager.AddAction(action.Object);


            testmanager.TestAction("MyAction", new Hashtable());
        }

        [TestMethod]
        [ExpectedException(typeof (IdempotentActionDoestnExist))]
        public void CallingTestActionWillThrowIfActionDesontExist()
        {
            var testmanager = new IdempotentActionManager();

            testmanager.TestAction("MyNonExistingAction", new Hashtable());
        }

        [TestMethod]
        [ExpectedException(typeof (IdempotentActionPropertyException))]
        public void CallingTestActionWithPropertyThatDoesntExistInRequiredOrOptionalWillThrow()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action.Setup(a => a.RequiredProperties).Returns(new[] { "MyReqProp" });
            action.Setup(a => a.OptionalProperties).Returns(new[] { "MyOpProp" });
            testmanager.AddAction(action.Object);


            testmanager.TestAction("MyAction", new Hashtable { { "MyReqProp", "value"}, { "MyOpProp", "value" }, { "OtherBadPropertyName", "value" } });
        }

        [TestMethod]
        public void CallingTestActionWillCallUnderlyingActionsTestMethod()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action.Setup(a => a.RequiredProperties).Returns(new string[] { });
            action.Setup(a => a.OptionalProperties).Returns(new string[] { });
            testmanager.AddAction(action.Object);


            testmanager.TestAction("MyAction", new Hashtable());

            action.Verify(a => a.TestAction(It.IsAny<Hashtable>()));
        }

        [TestMethod]
        public void CallingUpdateActionWillCallTestAction()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action.Setup(a => a.RequiredProperties).Returns(new string[] { });
            action.Setup(a => a.OptionalProperties).Returns(new string[] { });
            testmanager.AddAction(action.Object);
            
            testmanager.UpdateAction("MyAction", new Hashtable());
            action.Verify(a => a.TestAction(It.IsAny<Hashtable>()));
        }

        [TestMethod]
        public void CallingUpdateActionWillCallUnderlyingUpdateActionIfTestReturnedUnconfigured()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action.Setup(a => a.RequiredProperties).Returns(new string[] { });
            action.Setup(a => a.OptionalProperties).Returns(new string[] { });
            action.Setup(a => a.TestAction(It.IsAny<Hashtable>())).Returns(IdempotentActionResult.NotConfigured);
            testmanager.AddAction(action.Object);

            testmanager.UpdateAction("MyAction", new Hashtable());
            action.Verify(a => a.UpdateAction(It.IsAny<Hashtable>()));
        }

        [TestMethod]
        public void CallingUpdateActionWillCallNotCallUnderlyingUpdateActionIfReturnOk()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action.Setup(a => a.RequiredProperties).Returns(new string[] { });
            action.Setup(a => a.OptionalProperties).Returns(new string[] { });
            action.Setup(a => a.TestAction(It.IsAny<Hashtable>())).Returns(IdempotentActionResult.Ok);
            testmanager.AddAction(action.Object);

            testmanager.UpdateAction("MyAction", new Hashtable());
            action.Verify(a => a.UpdateAction(It.IsAny<Hashtable>()), Times.Never);
        }

        [TestMethod]
        public void CallingUpdateActionWillCallTestASecondTimeIfUpdateIsCalledToVerifyChangeHasHappened()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action.Setup(a => a.RequiredProperties).Returns(new string[] { });
            action.Setup(a => a.OptionalProperties).Returns(new string[] { });
            action.Setup(a => a.TestAction(It.IsAny<Hashtable>())).Returns(IdempotentActionResult.NotConfigured);
            testmanager.AddAction(action.Object);

            testmanager.UpdateAction("MyAction", new Hashtable());
            action.Verify(a => a.TestAction(It.IsAny<Hashtable>()), Times.Exactly(2));
        }

        [TestMethod]
        public void CallingUpdateActionWillReturnRebootIfUpdateReturnsReboot()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action.Setup(a => a.RequiredProperties).Returns(new string[] { });
            action.Setup(a => a.OptionalProperties).Returns(new string[] { });
            action.Setup(a => a.TestAction(It.IsAny<Hashtable>())).Returns(IdempotentActionResult.NotConfigured);
            action.Setup(a => a.UpdateAction(It.IsAny<Hashtable>())).Returns(IdempotentActionResult.RebootRequired);
            testmanager.AddAction(action.Object);

            Assert.IsTrue(testmanager.UpdateAction("MyAction", new Hashtable()) == IdempotentActionResult.RebootRequired);
        }

        [TestMethod]
        public void CallingUpdateActionWillReturnFailIfUpdateReturnsFail()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action.Setup(a => a.RequiredProperties).Returns(new string[] { });
            action.Setup(a => a.OptionalProperties).Returns(new string[] { });
            action.Setup(a => a.TestAction(It.IsAny<Hashtable>())).Returns(IdempotentActionResult.NotConfigured);
            action.Setup(a => a.UpdateAction(It.IsAny<Hashtable>())).Returns(IdempotentActionResult.Failed);
            testmanager.AddAction(action.Object);

            Assert.IsTrue(testmanager.UpdateAction("MyAction", new Hashtable()) == IdempotentActionResult.Failed);
        }

        [TestMethod]
        public void CallingUpdateActionWillOnlyCallTestOnceIfStatusOk()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            action.Setup(a => a.RequiredProperties).Returns(new string[] { });
            action.Setup(a => a.OptionalProperties).Returns(new string[] { });
            action.Setup(a => a.TestAction(It.IsAny<Hashtable>())).Returns(IdempotentActionResult.Ok);
            testmanager.AddAction(action.Object);

            testmanager.UpdateAction("MyAction", new Hashtable());
            action.Verify(a => a.TestAction(It.IsAny<Hashtable>()), Times.Once);
        }

        [TestMethod]
        public void CallingClearActionWillRemoveAllActions()
        {
            var testmanager = new IdempotentActionManager();

            var action = new Mock<IIdempotentAction>();
            action.Setup(a => a.Name).Returns("MyAction");
            testmanager.AddAction(action.Object);

            testmanager.ClearAction();

            Assert.IsTrue(testmanager.GetActions().Length == 0);
        }
    }
}
