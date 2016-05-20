using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VMLab.Helper;
using VMLab.Model;
using VMLab.Test.Helper.IOCTestObjects;

namespace VMLab.Test.Helper
{
    [TestClass]
    public class IocContainerTests
    {

        public IocContainer Container;

        [TestInitialize]
        public void Setup()
        {
            Container = new IocContainer();
        }

        [TestMethod]
        public void CanRegisterObjectWithContainer()
        {
            Container.Register<IRegularObject, RegularObject>();
        }

        [TestMethod]
        public void CanRetriveObjectsTypesThatHaveBeenRegistered()
        {
            Container.Register<IRegularObject, RegularObject>();

            Assert.IsInstanceOfType(Container.GetObject<IRegularObject>(), typeof(RegularObject));
        }

        [TestMethod]
        [ExpectedException(typeof(IocException))]
        public void CallingGetObjectOnATypeThatIsntRegisteredThrows()
        {
            Container.GetObject<IRegularObject>();
        }

        [TestMethod]
        [ExpectedException(typeof(IocException))]
        public void CallingGetObjectWhenMultipleAreRegisteredThrows()
        {
            Container.Register<IRegularObject, RegularObject>();
            Container.Register<IRegularObject, RegularObject2>();

            Container.GetObject<IRegularObject>();
        }

        [TestMethod]
        public void CallingGetObjectsWillReturnAnArrayOfObjectsRegistered()
        {
            Container.Register<IRegularObject, RegularObject>();
            Container.Register<IRegularObject, RegularObject2>();

            var data = Container.GetObjects<IRegularObject>().ToArray();

            Assert.IsTrue(data.Any(d => d.GetType().IsAssignableFrom(typeof(RegularObject))));
            Assert.IsTrue(data.Any(d => d.GetType().IsAssignableFrom(typeof(RegularObject2))));
        }

        [TestMethod]
        public void RegisteringAnObjectWithASetNameWillReturnWhenSetIsPassedToGetObject()
        {
            Container.Register<IRegularObject, RegularObject>("MySet");

            Assert.IsInstanceOfType(Container.GetObject<IRegularObject>("MySet"), typeof(RegularObject));
        }

        [TestMethod]
        [ExpectedException(typeof(IocException))]
        public void RegisteringAnObjectWithASetNameWillNotReturnIfGetObjectIsCalledWithoutSetName()
        {
            Container.Register<IRegularObject, RegularObject>("MySet");

            Container.GetObject<IRegularObject>();
        }

        [TestMethod]
        public void RegisteringAnObjectWithoutASetNameWillStillReturnWhenIfSetNameRequested()
        {
            Container.Register<IRegularObject, RegularObject>();

            Assert.IsInstanceOfType(Container.GetObject<IRegularObject>("MySet"), typeof(RegularObject));
        }

        [TestMethod]
        public void RegisteringObjectsWithASetNameWillReturnWhenSetIsPassedToGetObjects()
        {
            Container.Register<IRegularObject, RegularObject>("MySet");
            Container.Register<IRegularObject, RegularObject2>("MySet");

            var data = Container.GetObjects<IRegularObject>("MySet").ToArray();

            Assert.IsTrue(data.Any(d => d.GetType().IsAssignableFrom(typeof(RegularObject))));
            Assert.IsTrue(data.Any(d => d.GetType().IsAssignableFrom(typeof(RegularObject2))));
        }

        [TestMethod]
        public void RegisteringAnObjectWithADifferentSetNameToRequestedWillReturnEmptyArray()
        {
            Container.Register<IRegularObject, RegularObject>("MySet");
            Container.Register<IRegularObject, RegularObject2>("MySet");

            var data = Container.GetObjects<IRegularObject>("OtherSet").ToArray();

            Assert.IsTrue(data.Length == 0);
        }

        [TestMethod]
        public void RegisteringAnObjectWithNoSetWillReturnEvenWhenSetNameIsRequested()
        {
            Container.Register<IRegularObject, RegularObject>();
            Container.Register<IRegularObject, RegularObject2>();

            var data = Container.GetObjects<IRegularObject>("MySet").ToArray();

            Assert.IsTrue(data.Any(d => d.GetType().IsAssignableFrom(typeof(RegularObject))));
            Assert.IsTrue(data.Any(d => d.GetType().IsAssignableFrom(typeof(RegularObject2))));
        }

        [TestMethod]
        public void CallingRegisterInstanceWillAssignAnInstanceOfAnObjectToAType()
        {
            var obj = new RegularObject
            {
                Value1 = "Test Value"
            };

            Container.RegisterInstance<IRegularObject>(obj);

            var data = Container.GetObject<IRegularObject>();

            Assert.AreSame(obj, data);
        }

        [TestMethod]
        public void CallingRegisterInstanceMultipleTimesWillOnlyKeepTheLatestVersion()
        {
            var obj = new RegularObject
            {
                Value1 = "Test Value"
            };

            var obj2 = new RegularObject
            {
                Value1 = "Test Value2"
            };

            Container.RegisterInstance<IRegularObject>(obj);
            Container.RegisterInstance<IRegularObject>(obj2);

            var data = Container.GetObject<IRegularObject>();

            Assert.AreSame(obj2, data);
        }

        [TestMethod]
        public void CallingSingletonOnObjectRegistrationWillReturnSameObjectEachTimeGetObjectIsCalled()
        {
            Container.Register<IRegularObject, RegularObject>().Singleton();

            var obj1 = Container.GetObject<IRegularObject>();
            var obj2 = Container.GetObject<IRegularObject>();

            Assert.AreSame(obj1, obj2);   
        }


    }
}
