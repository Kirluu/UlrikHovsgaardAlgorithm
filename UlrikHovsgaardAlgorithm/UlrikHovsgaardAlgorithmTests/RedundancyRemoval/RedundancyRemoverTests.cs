using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithmTests.RedundancyRemoval
{
    [TestClass()]
    public class RedundancyRemoverTests
    {

        [TestMethod()]
        //Include: Hvis B kun kan køres efter A(enten via inclusion eller condition) og både A og B har en include relation til C, kan “B->+C” altid slettes.
        public void RedundancyTestCase1()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = false };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);
            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityC.Id);
            var newDcr = RedundancyRemover.RemoveRedundancy(dcrGraph);


            //we should now have removed include b -> c. so we are asserting that B no longer has a include relation
            Assert.IsFalse(newDcr.GetIncludeOrExcludeRelation(activityB, true).Contains(activityC));
        }
        
        [TestMethod()]
        //Include: condition A->B er redundant med include A->B, hvis B ikke har andre indgående includes og A ikke kan blive ekskluderet. 
        public void RedundancyTestCase2()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = false };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);

            var newGraph = RedundancyRemover.RemoveRedundancy(dcrGraph);

            //Now the redundant condition relation from A to B should be removed:
            Assert.IsFalse(newGraph.InRelation(activityB, newGraph.Conditions));
            
        }
        
        [TestMethod()]
        //Include: Response og Exclude fra samme source til samme target er i udgangspunkt redundante, hvis target ikke har nogle indgående include relationer.
        public void RedundancyTestCase3()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.AddResponse(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);

            var newGraph = RedundancyRemover.RemoveRedundancy(dcrGraph);

            //Now the redundant response relation should be removed:

            Assert.IsFalse(newGraph.InRelation(activityB, newGraph.Responses));
        }

        [TestMethod()]
        //Hvis en aktivitet er included og ikke har nogle indgående exclude-relationer, er indgående include-relationer redundante. 
        public void RedundancyTestCase4()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);
            
            //Now the redundant include relation should be removed:");
            
            var newGraph = RedundancyRemover.RemoveRedundancy(dcrGraph);
            Assert.IsFalse(newGraph.InRelation(activityA, newGraph.IncludeExcludes));
        }

        [TestMethod()]
        //Condition og milestone er redundante med hinanden fra A til B, hvis A er excluded og altid bliver sat som pending, når den bliver inkluderet. 
        public void RedundancyTestCase5()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddMileStone(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityA.Id);
            dcrGraph.AddResponse(activityC.Id, activityA.Id);

            var newGraph = RedundancyRemover.RemoveRedundancy(dcrGraph);

            //Now either the redundant Condition or Milestone relation should be removed:");
            Assert.IsFalse(newGraph.InRelation(activityA, newGraph.Conditions) && newGraph.InRelation(activityA, newGraph.Milestones));
        }

        [TestMethod()]
        //Ikke-included og ingen include relationer med den som target, gør det er redundant at den (og alle dens udadgående relationer) er medtaget i grafen. 
        public void RedundancyTestCase6()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddMileStone(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityC.Id);
            dcrGraph.AddResponse(activityA.Id, activityB.Id);
            dcrGraph.AddResponse(activityB.Id, activityA.Id);

            var newGraph = RedundancyRemover.RemoveRedundancy(dcrGraph);

            //Now all the relations from or to A should be removed:");
            Assert.IsTrue(!newGraph.InRelation(activityA, newGraph.Conditions)
                && !newGraph.InRelation(activityA, newGraph.IncludeExcludes)
                && !newGraph.InRelation(activityA, newGraph.Responses)
                && !newGraph.InRelation(activityA, newGraph.Milestones));
        }

        /*
        //Hvis A er pending og excluded, og der er en response og include relation fra B til A, så er pending og response redundante med hinanden, hvis B ikke kan køres efter A. 
        public void RedundancyTestCase7()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false, Pending = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddResponse(activityB.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);
            Console.WriteLine("The initial Test Case 7 graph before redundancy removal:");
            Console.WriteLine(dcrGraph);

            Console.WriteLine("\nNow either the redundant response relation or A's initial pending state should be removed:");
            //TODO: Assert
            Console.WriteLine(RedundancyRemover.RemoveRedundancy(dcrGraph));
            Console.ReadLine();
        }
        //TODO: test that non-redundant relations are not removed.

        */
    }
}