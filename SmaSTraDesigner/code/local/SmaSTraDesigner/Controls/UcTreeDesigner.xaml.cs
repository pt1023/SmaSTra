﻿namespace SmaSTraDesigner.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Navigation;
	using System.Windows.Shapes;

	using Common;
	using Common.ExtensionMethods;
	using Common.Resources.Converters;

	using SmaSTraDesigner.BusinessLogic;

	// TODO: (PS) Comment this.
	// TODO: (PS) Adapt for dynamic size changes for canvas.
	/// <summary>
	/// Interaction logic for UcTreeDesigner.xaml
	/// </summary>
	public partial class UcTreeDesigner : UserControl
	{
		#region static constructor

		static UcTreeDesigner()
		{
			ConnectingIOHandleProperty = ConnectingIOHandlePropertyKey.DependencyProperty;
		}

		#endregion static constructor

		#region static methods

		private static double GetOffset(double size, double viewPortSize)
		{
			return (size - viewPortSize) / 2.0;
		}

		#endregion static methods

		#region dependency properties

		/// <summary>
		/// Registration of ConnectingIOHandle Dependency Property.
		/// </summary>
		public static readonly DependencyProperty ConnectingIOHandleProperty;

		/// <summary>
		/// Registration of ConnectingIOHandle Dependency Property Key.
		/// </summary>
		private static readonly DependencyPropertyKey ConnectingIOHandlePropertyKey = 
			DependencyProperty.RegisterReadOnly("ConnectingIOHandle", typeof(UcIOHandle), typeof(UcTreeDesigner), new FrameworkPropertyMetadata(null));

		/// <summary>
		/// Registration of SelectedNodeViewer Dependency Property.
		/// </summary>
		public static readonly DependencyProperty SelectedNodeViewerProperty = 
			DependencyProperty.Register(
				"SelectedNodeViewer", typeof(UcNodeViewer), typeof(UcTreeDesigner),
				new FrameworkPropertyMetadata(
					null,
					OnSelectedNodeViewerChanged));

		/// <summary>
		/// Registration of Tree Dependency Property.
		/// </summary>
		public static readonly DependencyProperty TreeProperty = 
			DependencyProperty.Register(
				"Tree", typeof(TransformationTree), typeof(UcTreeDesigner),
				new FrameworkPropertyMetadata(
					null,
					OnTreeChanged));

		#endregion dependency properties

		#region dependency property callbacks

		/// <summary>
		/// Property Changed Callback method of the SelectedNodeViewer Dependency Property.
		/// </summary>
		/// <param name="sender">The instance of the class that had the SelectedNodeViewer property changed.</param>
		/// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		private static void OnSelectedNodeViewerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			UcTreeDesigner subject = (UcTreeDesigner)sender;
			UcNodeViewer newValue = (UcNodeViewer)e.NewValue;
			UcNodeViewer oldValue = (UcNodeViewer)e.OldValue;

			if (!subject.changingSelectedNodeViewers)
			{
				subject.SelectedNodeViewers.CollectionChanged -= subject.SelectedNodeViewers_CollectionChanged;

				foreach (var noveViewer in subject.SelectedNodeViewers)
				{
					noveViewer.IsSelected = false;
				}

				subject.SelectedNodeViewers.Clear();
				if (newValue != null)
				{
					subject.SelectedNodeViewers.Add(newValue);
					newValue.IsSelected = true;
				}

				subject.previouslySelectedItems = subject.SelectedNodeViewers.ToArray();

				subject.AdjustZIndex();

				subject.SelectedNodeViewers.CollectionChanged += subject.SelectedNodeViewers_CollectionChanged;
			}
		}

		/// <summary>
		/// Property Changed Callback method of the Tree Dependency Property.
		/// </summary>
		/// <param name="sender">The instance of the class that had the Tree property changed.</param>
		/// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		private static void OnTreeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			UcTreeDesigner subject = (UcTreeDesigner)sender;
			TransformationTree newValue = (TransformationTree)e.NewValue;
			TransformationTree oldValue = (TransformationTree)e.OldValue;

			subject.ShowTree(newValue);
		}

		#endregion dependency property callbacks

		#region fields

		private LambdaConverter canvasOffsetConverter;
		private bool changingSelectedNodeViewers = false;
		private Point? dragStart = null;
		private Point? mousePosOnViewer = null;
		private UcNodeViewer movingNodeViewer = null;
		private HashSet<Node> nodes = new HashSet<Node>();
		private UcNodeViewer[] previouslySelectedItems = { };
		private HashSet<UcIOHandle> registeredIoHandles = new HashSet<UcIOHandle>();

		#endregion fields

		#region constructors

		public UcTreeDesigner()
		{
			this.SelectedNodeViewers = new ObservableCollection<UcNodeViewer>();
			this.SelectedNodeViewers.CollectionChanged += SelectedNodeViewers_CollectionChanged;

			this.InitializeComponent();

			Panel.SetZIndex(this.bdrSelectionBorder, Int32.MaxValue);
			Panel.SetZIndex(this.linPreviewConnection, Int32.MaxValue);

			this.cnvBackground.Width = 10000;
			this.cnvBackground.Height = 10000;

			this.outOutputViewer.DataContext = new OutputNode();

			this.canvasOffsetConverter = new LambdaConverter()
			{
				MultiConvertMethod = (values, targetType, parameter, culture) =>
				{
					if (values.Any(v => !(v is double) || Double.IsNaN((double)v)))
					{
						return null;
					}

					double controlSize = (double)values[0];
					double canvasSize = (double)values[1];
					double offset = (double)values[2];

					return GetOffset(canvasSize, controlSize) + offset;
				}
			};

			this.MakeBindings(this.outOutputViewer);
		}

		#endregion constructors

		#region properties

		/// <summary>
		/// Gets the value of the ConnectingIOHandle property.
		/// This is a Dependency Property.
		/// </summary>
		public UcIOHandle ConnectingIOHandle
		{
			get { return (UcIOHandle)this.GetValue(ConnectingIOHandleProperty); }
			private set { this.SetValue(ConnectingIOHandlePropertyKey, value); }
		}

		/// <summary>
		/// Gets or sets the value of the SelectedNodeViewer property.
		/// TODO: (PS) Comment this.
		/// This is a Dependency Property.
		/// </summary>
		public UcNodeViewer SelectedNodeViewer
		{
			get { return (UcNodeViewer)this.GetValue(SelectedNodeViewerProperty); }
			set { this.SetValue(SelectedNodeViewerProperty, value); }
		}

		// TODO: (PS) Comment this.
		public ObservableCollection<UcNodeViewer> SelectedNodeViewers
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the value of the Tree property.
		/// TODO: (PS) Comment this.
		/// This is a Dependency Property.
		/// </summary>
		public TransformationTree Tree
		{
			get { return (TransformationTree)this.GetValue(TreeProperty); }
			set { this.SetValue(TreeProperty, value); }
		}

		#endregion properties

		#region methods

		public void AddNode(Node node, bool select = false)
		{
			if (this.nodes.Contains(node))
			{
				return;
			}

			Transformation nodeAsTransformation;
			//DataSource nodeAsDataSource;
			UcNodeViewer nodeViewer;
			if ((nodeAsTransformation = node as Transformation) != null)
			{
				nodeViewer = new UcTransformationViewer();
			}
			else // if ((nodeAsDataSource = node as DataSource) != null)
			{
				nodeViewer = new UcDataSourceViewer();
			}

			nodeViewer.DataContext = node;
			this.cnvBackground.Children.Add(nodeViewer);
			this.MakeBindings(nodeViewer);

			if (select)
			{
				nodeViewer.IsSelected = true;
				nodeViewer.BringIntoView();
			}
		}

		// TODO: (PS) Remove this.
		public void UseTestTree()
		{
		}

		internal void RegisterIOHandle(UcIOHandle ioHandle)
		{
			ioHandle.CustomDrag += IoHandle_CustomDrag;
			this.registeredIoHandles.Add(ioHandle);
		}

		private void AdjustZIndex()
		{
			int i = 0;
			foreach (var nodeViewer in this.cnvBackground.Children.OfType<UcNodeViewer>().OrderBy(Panel.GetZIndex))
			{
				Panel.SetZIndex(nodeViewer, i++);
			}

			if (this.SelectedNodeViewers.Count != 0)
			{
				i = Int32.MaxValue - 2;
				foreach (var nodeViewer in this.SelectedNodeViewers.OrderBy(Panel.GetZIndex))
				{
					Panel.SetZIndex(nodeViewer, i--);
				}

				Panel.SetZIndex(this.SelectedNodeViewers.First(), Int32.MaxValue - 1);
			}
		}

		private void MakeBindings(UcNodeViewer nodeViewer)
		{
			this.MakeCanvasOffsetBinding(nodeViewer, false);
			this.MakeCanvasOffsetBinding(nodeViewer, true);

			((UcNodeViewer)nodeViewer).StartedMoving += UcNodeViewer_StartedMoving;
		}

		private void MakeCanvasOffsetBinding(UcNodeViewer nodeViewer, bool isVertical)
		{
			MultiBinding binding = new MultiBinding()
			{
				Converter = this.canvasOffsetConverter
			};

			DependencyProperty property;
			if (isVertical)
			{
				property = Canvas.TopProperty;
				binding.AddBindings(
					new Binding("ActualHeight") { RelativeSource = RelativeSource.Self },
					new Binding("ActualHeight") { Source = this.cnvBackground },
					new Binding("PosY"));
			}
			else
			{
				property = Canvas.LeftProperty;
				binding.AddBindings(
					new Binding("ActualWidth") { RelativeSource = RelativeSource.Self },
					new Binding("ActualWidth") { Source = this.cnvBackground },
					new Binding("PosX"));
			}

			BindingOperations.SetBinding(nodeViewer, property, binding);
		}

		private void MarkNodesInSelectionArea(bool select)
		{
			IEnumerable<UcNodeViewer> nodeViewers = this.cnvBackground.Children.OfType<UcNodeViewer>();
			if (select)
			{
				nodeViewers = nodeViewers.OrderByDescending(Panel.GetZIndex);
			}

			foreach (var nodeViewer in nodeViewers)
			{
				double centerX = Canvas.GetLeft(nodeViewer) + nodeViewer.ActualWidth / 2;
				double centerY = Canvas.GetTop(nodeViewer) + nodeViewer.ActualHeight / 2;
				Rect selectionBorder = new Rect(new Point(Canvas.GetLeft(this.bdrSelectionBorder), Canvas.GetTop(this.bdrSelectionBorder)), this.bdrSelectionBorder.RenderSize);
				bool inside = selectionBorder.Contains(centerX, centerY);
				nodeViewer.IsInSelectionArea = !select && inside;
				if (inside && select)
				{
					this.SelectedNodeViewers.Add(nodeViewer);
				}
			}
		}

		private void ShowTree(TransformationTree tree)
		{
			this.cnvBackground.Children.Clear();
			this.cnvBackground.Children.Add(this.bdrSelectionBorder);
			this.nodes.Clear();
			if (tree != null)
			{
				this.cnvBackground.Children.Add(this.outOutputViewer);
				this.outOutputViewer.DataContext = tree.OutputNode;
			}
		}

		private void StopDragging(MouseButtonEventArgs e)
		{
			this.mousePosOnViewer = null;
			this.dragStart = null;

			this.movingNodeViewer = null;
			if (this.bdrSelectionBorder.Visibility == Visibility.Visible)
			{
				this.MarkNodesInSelectionArea(true);

				this.bdrSelectionBorder.Visibility = Visibility.Collapsed;
			}
			if (this.ConnectingIOHandle != null)
			{
				this.linPreviewConnection.Visibility = Visibility.Collapsed;

				this.TryToConnect(e);

				this.ConnectingIOHandle = null;
			}
		}

		private void TryToConnect(MouseButtonEventArgs e)
		{
			if (e != null && this.ConnectingIOHandle != null)
			{
				Point mousePos = e.GetPosition(this.cnvBackground);
				UcIOHandle handleUnderCursor = null;
				int zIndex = Int32.MinValue;
				foreach (var ioHandle in this.registeredIoHandles)
				{
					if (ioHandle != this.ConnectingIOHandle)
					{
						Point handlePos = ioHandle.TransformToAncestor(this.cnvBackground).Transform(new Point());
						if (new Rect(handlePos, ioHandle.RenderSize).Contains(mousePos) &&
							(handleUnderCursor == null || Panel.GetZIndex(ioHandle) > zIndex))
						{
							zIndex = Panel.GetZIndex(ioHandle);
							handleUnderCursor = ioHandle;
						}
					}
				}

				if (handleUnderCursor != null)
				{
					UcIOHandle iHandle, oHandle;
					if (handleUnderCursor.IsInput)
					{
						iHandle = handleUnderCursor;
						oHandle = this.ConnectingIOHandle;
					}
					else
					{
						iHandle = this.ConnectingIOHandle;
						oHandle = handleUnderCursor;
					}

					iHandle.Node.Tree.Connections.Add(new Tuple<Node, Node, int>(oHandle.Node, iHandle.Node, iHandle.InputIndex));
				}
			}
		}

		#endregion methods

		#region event handlers

		private void cnvBackground_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			this.SelectedNodeViewers.Clear();
		}

		private void cnvBackground_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			this.SelectedNodeViewers.Clear();
		}

		private void IoHandle_CustomDrag(object sender, EventArgs e)
		{
			this.ConnectingIOHandle = (UcIOHandle)sender;
		}

		private void SelectedNodeViewers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			this.changingSelectedNodeViewers = true;

			this.SelectedNodeViewer = this.SelectedNodeViewers.Count != 0 ?
				this.SelectedNodeViewers.First() :
				null;

			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				foreach (UcNodeViewer nodeViewer in this.previouslySelectedItems)
				{
					nodeViewer.IsSelected = false;
				}
			}
			else
			{
				if (e.NewItems != null)
				{
					foreach (UcNodeViewer nodeViewer in e.NewItems.Cast<UcNodeViewer>())
					{
						nodeViewer.IsSelected = true;
					}
				}
				if (e.OldItems != null)
				{
					foreach (UcNodeViewer nodeViewer in e.OldItems.Cast<UcNodeViewer>())
					{
						nodeViewer.IsSelected = false;
					}
				}

				this.AdjustZIndex();
			}

			this.previouslySelectedItems = this.SelectedNodeViewers.ToArray();

			this.changingSelectedNodeViewers = false;
		}

		private void This_DragOver(object sender, DragEventArgs e)
		{
		}

		private void This_Drop(object sender, DragEventArgs e)
		{
			Node node = ((Tuple<Node>)e.Data.GetData(typeof(Tuple<Node>))).Item1;
			Point mousePos = e.GetPosition(this.cnvBackground);
			Node newNode = (Node)node.Clone();
			newNode.PosX = mousePos.X - this.cnvBackground.ActualWidth / 2;
			newNode.PosY = mousePos.Y - this.cnvBackground.ActualHeight / 2;

			this.AddNode(newNode, true);
		}

		private void This_Loaded(object sender, RoutedEventArgs e)
		{
			this.scvCanvas.ScrollToHorizontalOffset(GetOffset(this.vbBackground.ActualWidth, this.scvCanvas.ViewportWidth));
			this.scvCanvas.ScrollToVerticalOffset(GetOffset(this.vbBackground.ActualHeight, this.scvCanvas.ViewportHeight));
		}

		private void This_MouseLeave(object sender, MouseEventArgs e)
		{
			this.StopDragging(null);
		}

		private void This_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Point mousePos = e.GetPosition(this.cnvBackground);
				if (this.dragStart == null)
				{
					this.dragStart = mousePos;
				}

				if (this.movingNodeViewer != null)
				{
					if (this.mousePosOnViewer == null)
					{
						this.mousePosOnViewer = Mouse.GetPosition(this.movingNodeViewer);
					}

					Node node = (Node)this.movingNodeViewer.DataContext;
					double dx = mousePos.X - this.mousePosOnViewer.Value.X + (this.movingNodeViewer.ActualWidth - this.cnvBackground.ActualWidth) / 2 - node.PosX;
					double dy = mousePos.Y - this.mousePosOnViewer.Value.Y + (this.movingNodeViewer.ActualHeight - this.cnvBackground.ActualHeight) / 2 - node.PosY;
					foreach (var nodeViewer in this.SelectedNodeViewers)
					{
						node = (Node)nodeViewer.DataContext;
						node.PosX += dx;
						node.PosY += dy;
					}
				}
				else if (this.ConnectingIOHandle != null)
				{
					this.linPreviewConnection.Visibility = Visibility.Visible;

					Point pos = this.ConnectingIOHandle.TransformToAncestor(this.cnvBackground)
						.Transform(new Point(this.ConnectingIOHandle.ActualWidth / 2, this.ConnectingIOHandle.ActualHeight / 2));
					this.linPreviewConnection.X1 = pos.X;
					this.linPreviewConnection.Y1 = pos.Y;
					this.linPreviewConnection.X2 = mousePos.X;
					this.linPreviewConnection.Y2 = mousePos.Y;
				}
				else
				{
					this.bdrSelectionBorder.Visibility = Visibility.Visible;
					double dx = mousePos.X - this.dragStart.Value.X;
					double dy = mousePos.Y - this.dragStart.Value.Y;
					double x = this.dragStart.Value.X;
					if (dx < 0)
					{
						x += dx;
					}
					double y = this.dragStart.Value.Y;
					if (dy < 0)
					{
						y += dy;
					}

					Canvas.SetLeft(this.bdrSelectionBorder, x);
					Canvas.SetTop(this.bdrSelectionBorder, y);
					this.bdrSelectionBorder.Width = Math.Abs(dx);
					this.bdrSelectionBorder.Height = Math.Abs(dy);

					this.MarkNodesInSelectionArea(false);
				}
			}
		}

		private void This_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this.StopDragging(e);
		}

		private void UcNodeViewer_StartedMoving(object sender, EventArgs e)
		{
			this.movingNodeViewer = (UcNodeViewer)sender;
		}

		#endregion event handlers
	}
}