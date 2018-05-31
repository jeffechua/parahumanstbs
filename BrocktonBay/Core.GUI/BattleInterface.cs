﻿using System;
using Gtk;
using System.Collections.Generic;

namespace Parahumans.Core.GUI {

	public class DeploymentPlanner : HBox, IDependable {

		public int order { get { return 6; } }

		public List<WeakReference<IDependable>> dependents { get; set; } = new List<WeakReference<IDependable>>();
		public List<WeakReference<IDependable>> dependencies { get; set; } = new List<WeakReference<IDependable>>();

		Deployment deployment;

		public DeploymentPlanner (Deployment dep) : base(false, 10) {

			deployment = dep;
			DependencyManager.Connect(deployment, this);

			Search search1 = new Search(deployment.Accepts, Push);
			Search search2 = new Search(deployment.Contains, Pull);
			DependencyManager.Connect(this, search1);
			DependencyManager.Connect(this, search2);
			PackStart(search1);
			PackStart(search2);

		}

		public void Reload () {}

		public void Push (GameObject obj) {
			deployment.AddRange(new List<GameObject> {obj});
			DependencyManager.TriggerAllFlags();
		}

		public void Pull (GameObject obj) {
			deployment.RemoveRange(new List<GameObject> { obj });
			DependencyManager.TriggerAllFlags();
		}

	}
}