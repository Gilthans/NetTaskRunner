using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace NetTaskRunner.Tests
{
	[TestClass]
	public class ArgumentHolderTests
	{
		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void RegisterResult_ShouldFailWhenAddingExistingName()
		{
			// Arrange
			var argumentHolder = new ArgumentHolder();
			argumentHolder.RegisterResult("Misha", 5);

			// Act
			argumentHolder.RegisterResult("Misha", "Jelly");
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void RegisterResult_ShouldFailWhenAddingExistingType()
		{
			// Arrange
			var argumentHolder = new ArgumentHolder();
			argumentHolder.RegisterResult("Misha", 5);

			// Act
			argumentHolder.RegisterResult("Rambo", 7);
		}

		[TestMethod]
		public void RegisterResult_RegisteringNullValueShouldNotAddValue()
		{
			// Arrange
			var argumentHolder = new ArgumentHolder();
			argumentHolder.RegisterResult("Misha", null);

			// Assert
			Assert.AreEqual(0, argumentHolder.Count);
			try
			{
				argumentHolder.Get("Misha");
				Assert.Fail("Expected argument exception");
			}
			catch (KeyNotFoundException) { }
		}

		[TestMethod]
		public void Count_ShouldHaveCorrectTallyAfterRegistering()
		{
			// Arrange
			var argumentHolder = new ArgumentHolder();
			argumentHolder.RegisterResult("Misha", 5);
			argumentHolder.RegisterResult("Rambo", "Who");
			argumentHolder.RegisterResult("Pi", 3.14);

			// Assert
			Assert.AreEqual(3, argumentHolder.Count);
		}

		[TestMethod]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void Get_ByInexistentNameShouldThrowException()
		{
			// Arrange
			var argumentHolder = new ArgumentHolder();
			argumentHolder.RegisterResult("Misha", 5);

			// Act
			argumentHolder.Get("Rambo");
		}

		[TestMethod]
		public void Get_ByExistingNameShouldReturnAddedValue()
		{
			// Arrange
			var argumentHolder = new ArgumentHolder();
			argumentHolder.RegisterResult("Misha", 5);

			// Act
			var result = argumentHolder.Get("Misha");

			// Assert
			Assert.IsInstanceOfType(result, typeof(int));
			Assert.AreEqual(5, result);
		}

		[TestMethod]
		public void Get_ByPreciseTypeShouldReturnValue()
		{
			// Arrange
			var argumentHolder = new ArgumentHolder();
			argumentHolder.RegisterResult("Misha", 5);

			// Act
			var result = argumentHolder.Get<int>();

			// Assert
			Assert.AreEqual(5, result);
		}

		#region Clear

		[TestMethod]
		public void Clear_ShouldRemoveAllOldValues()
		{
			// Arrange
			var argumentHolder = new ArgumentHolder();
			argumentHolder.RegisterResult("Misha", 5);

			// Act
			argumentHolder.Clear();

			// Assert
			Assert.AreEqual(0, argumentHolder.Count);
			try
			{
				argumentHolder.Get("Misha");
				Assert.Fail("Expected exception key not found!");
			}
			catch (KeyNotFoundException) { }
			try
			{
				argumentHolder.Get<int>();
				Assert.Fail("Expected exception key not found!");
			}
			catch (KeyNotFoundException) { }
		}

		#endregion
	}
}
