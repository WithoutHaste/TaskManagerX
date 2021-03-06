﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tamarin
{
	public class TaskTableControl : CoTableLayoutPanel
	{
		public TaskTableToolStrip ToolStrip { get; set; }

		private Project project;
		private History history;

		private bool showActive = true;

		private static int PLUS_COLUMN_INDEX = 0;
		private static int ROW_COLUMN_INDEX = 1;
		private static int ID_COLUMN_INDEX = 2;
		private static int TITLE_COLUMN_INDEX = 3;
		private static int STATUS_COLUMN_INDEX = 4;
		private static int CATEGORY_COLUMN_INDEX = 5;
		private static int CREATED_COLUMN_INDEX = 6;
		private static int DONE_COLUMN_INDEX = 7;
		private static int DELETE_COLUMN_INDEX = 8;
		private static int HEADER_ROW_INDEX = 0;

		private static float COLUMN_HIDDEN_WIDTH = 0F;
		private static float ID_COLUMN_HIDDEN_WIDTH = 5F;
		private static float ID_COLUMN_WIDTH = 45F;
		private static float CATEGORY_COLUMN_WIDTH = 100F;

		private bool WaitingOnLayoutToAddRows = false;

		private bool DisplayCategories {
			get {
				return (project.Categories.Length > 1);
			}
		}

		public TaskTableControl(Project project)
		{
			this.project = project;
			this.history = new History();

			this.Location = new Point(0, 0);
			this.Padding = new Padding(left: 0, top: 0, right: SystemInformation.VerticalScrollBarWidth, bottom: 0); //leave room for vertical scrollbar
			this.Dock = DockStyle.Fill;
			this.BackColor = Color.White;
			this.AutoScroll = true;
			this.VisibleChanged += new EventHandler(taskTableControl_VisibleChanged);

			ShowTaskSheet(active: showActive, forced: true);
		}

		private void taskTableControl_VisibleChanged(object sender, EventArgs e)
		{
			if(!(sender as Control).Visible)
				return;
			CheckForOutsideEdits();
		}

		public void CheckForOutsideEdits()
		{
			if(project.EditedByOutsideSource)
			{
				DialogResult result = MessageBox.Show(project.Name + " has been edited by an outside source. Reload?\nYou will lose any changed since your last save.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				if(result == DialogResult.Yes)
				{
					ReloadProject();
				}
			}
		}

		private void ReloadProject()
		{
			history.Clear();
			project.ReloadProject();
			ShowTaskSheet(showActive, forced:true);
		}

		public void Undo()
		{
			if(!history.CanUndo)
				return;
			HistoryAction action = history.Undo();
			action.Undo(this);
		}

		public void Redo()
		{
			if(!history.CanRedo)
				return;
			HistoryAction action = history.Redo();
			action.Redo(this);
		}

		public void ShowTaskSheet(bool active, bool forced = false)
		{
			if(!forced && showActive == active)
				return;

			RequestSuspendLayout();

			showActive = active;

			this.Controls.Clear();
			this.ColumnStyles.Clear();

			InsertTitleRow();
			ShowHideTaskIds();
			ShowHideCategories();

			WaitingOnLayoutToAddRows = true;
			RequestResumeLayout();
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);

			if(WaitingOnLayoutToAddRows)
			{
				WaitingOnLayoutToAddRows = false;

				RequestSuspendLayout();
				int row = 1;
				foreach(Task task in project.GetTasks(active: showActive))
				{
					InsertTaskRowAt(row, task);
					row++;
				}
				SetTabIndexes();
				RequestResumeLayout();
			}
		}

		public void ShowHideTaskIds()
		{
			try
			{
				if(Properties.Settings.Default.ShowTaskIds)
					ShowTaskIds();
				else
					HideTaskIds();
			}
			catch
			{
				HideTaskIds();
			}
		}

		private void ShowTaskIds()
		{
			this.ColumnStyles[ID_COLUMN_INDEX].Width = ID_COLUMN_WIDTH;
		}

		private void HideTaskIds()
		{
			this.ColumnStyles[ID_COLUMN_INDEX].Width = ID_COLUMN_HIDDEN_WIDTH;
		}

		public void ShowHideCategories()
		{
			if(DisplayCategories)
				ShowCategories();
			else
				HideCategories();
		}

		private void ShowCategories()
		{
			this.ColumnStyles[CATEGORY_COLUMN_INDEX].Width = CATEGORY_COLUMN_WIDTH;
		}

		private void HideCategories()
		{
			this.ColumnStyles[CATEGORY_COLUMN_INDEX].Width = COLUMN_HIDDEN_WIDTH;
		}

		public void InsertTitleRow()
		{
			this.Controls.Add(NewButton("+", addTask_Click), PLUS_COLUMN_INDEX, HEADER_ROW_INDEX);
			this.Controls.Add(new TitleLabel("Row"), ROW_COLUMN_INDEX, HEADER_ROW_INDEX);
			this.Controls.Add(new TitleLabel("Id"), ID_COLUMN_INDEX, HEADER_ROW_INDEX);
			this.Controls.Add(new TitleLabel("Description"), TITLE_COLUMN_INDEX, HEADER_ROW_INDEX);
			this.Controls.Add(new TitleLabel("Status"), STATUS_COLUMN_INDEX, HEADER_ROW_INDEX);
			this.Controls.Add(new TitleLabel("Category"), CATEGORY_COLUMN_INDEX, HEADER_ROW_INDEX);
			this.Controls.Add(new TitleLabel("Created"), CREATED_COLUMN_INDEX, HEADER_ROW_INDEX);
			this.Controls.Add(new TitleLabel("Finished"), DONE_COLUMN_INDEX, HEADER_ROW_INDEX);

			this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 45F));
			this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, ID_COLUMN_WIDTH));
			this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
			this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, (DisplayCategories ? COLUMN_HIDDEN_WIDTH : CATEGORY_COLUMN_WIDTH)));
			this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
			this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, (showActive ? COLUMN_HIDDEN_WIDTH : 80F)));
			this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));

			this.ColumnCount = DELETE_COLUMN_INDEX + 1;
			this.RowCount = HEADER_ROW_INDEX + 1;
		}

		public void EditStatuses()
		{
			using(StatusForm statusForm = new StatusForm(project.ActiveStatuses, project.InactiveStatuses))
			{
				DialogResult result = statusForm.ShowDialog();
				if(result != DialogResult.OK)
					return;
				try
				{
					project.SetStatuses(statusForm.GetActiveStatuses(), statusForm.GetInactiveStatuses());
				}
				catch(Exception e)
				{
					MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}
				UpdateStatusComboBoxOptions();
			}
		}

		private void UpdateStatusComboBoxOptions()
		{
			for(int row = 1; row <= this.RowCount; row++)
			{
				Control control = this.GetControlFromPosition(STATUS_COLUMN_INDEX, row);
				if(!(control is StatusComboBox))
					continue;
				(control as StatusComboBox).UpdateOptions(project.Statuses.ToList());
			}
		}

		public void EditCategories()
		{
			using(CategoryForm categoryForm = new CategoryForm(project.Categories))
			{
				DialogResult result = categoryForm.ShowDialog();
				if(result != DialogResult.OK)
					return;
				try
				{
					project.Categories = categoryForm.GetCategories();
				}
				catch(Exception e)
				{
					MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}
				UpdateCategoryComboBoxOptions();
				ShowHideCategories();
			}
		}

		private void UpdateCategoryComboBoxOptions()
		{
			for(int row = 1; row <= this.RowCount; row++)
			{
				Control control = this.GetControlFromPosition(CATEGORY_COLUMN_INDEX, row);
				if(!(control is CategoryComboBox))
					continue;
				(control as CategoryComboBox).UpdateOptions(project.Categories.ToList());
			}
		}

		private void InsertTaskRowAt(int rowIndex, Task task)
		{
			RequestSuspendLayout();

			InsertRowAt(rowIndex);

			Button addButton = NewButton("+", addTask_Click);
			this.Controls.Add(addButton, PLUS_COLUMN_INDEX, rowIndex);
			
			TextBox rowNumberBox = NewTextBox("RowNumberTextBox", rowIndex.ToString());
			rowNumberBox.LostFocus += new EventHandler(rowNumberTextBox_LostFocus);
			rowNumberBox.Margin = new Padding(0);
			rowNumberBox.TabStop = false;
			rowNumberBox.KeyDown += new KeyEventHandler(rowNumberTextBox_KeyDown);
			rowNumberBox.KeyUp += new KeyEventHandler(rowNumberTextBox_KeyUp);
			this.Controls.Add(rowNumberBox, ROW_COLUMN_INDEX, rowIndex);
			
			this.Controls.Add(NewDataLabel("Id", task.Id.ToString()), ID_COLUMN_INDEX, rowIndex);
			
			TitleTextBox titleBox = new TitleTextBox("TitleTextBox", task.Description);
			titleBox.GotFocus += new EventHandler(titleTextBox_GotFocus);
			titleBox.TextChanged += new EventHandler(titleTextBox_TextChanged);
			titleBox.KeyDown += new KeyEventHandler(titleTextBox_KeyDown);
			titleBox.KeyUp += new KeyEventHandler(titleTextBox_KeyUp);
			titleBox.TabIndex = 1;
			this.Controls.Add(titleBox, TITLE_COLUMN_INDEX, rowIndex);
			
			StatusComboBox statusComboBox = new StatusComboBox(project.Statuses.ToList(), task.Status);
			statusComboBox.SelectedIndexChanged += new EventHandler(statusComboBox_SelectedIndexChanged);
			this.Controls.Add(statusComboBox, STATUS_COLUMN_INDEX, rowIndex);

			CategoryComboBox categoryComboBox = new CategoryComboBox(project.Categories.ToList(), task.Category);
			categoryComboBox.SelectedIndexChanged += new EventHandler(categoryComboBox_SelectedIndexChanged);
			this.Controls.Add(categoryComboBox, CATEGORY_COLUMN_INDEX, rowIndex);

			Label createLabel = NewDataLabel("CreateDate", task.CreateDateString);
			createLabel.Margin = new Padding(0);
			createLabel.TextAlign = ContentAlignment.TopRight;
			createLabel.Dock = DockStyle.Right;
			this.Controls.Add(createLabel, CREATED_COLUMN_INDEX, rowIndex);

			Label doneLabel = NewDataLabel("DoneDate", task.DoneDateString);
			doneLabel.Margin = new Padding(0);
			doneLabel.TextAlign = ContentAlignment.TopRight;
			doneLabel.Dock = DockStyle.Right;
			this.Controls.Add(doneLabel, DONE_COLUMN_INDEX, rowIndex);

			this.Controls.Add(NewButton("X", deleteTask_Click), DELETE_COLUMN_INDEX, rowIndex);
			
			this.RowCount++;

			SetTabIndexes();

			RequestResumeLayout();
		}

		private void InsertRowAt(int rowIndex)
		{
			foreach(Control control in this.Controls)
			{
				int controlRow = this.GetRow(control);
				if(controlRow < rowIndex)
					continue;

				this.SetRow(control, controlRow + 1);

				if(control.Name == "RowNumberTextBox")
				{
					(control as TextBox).Text = (Int32.Parse((control as TextBox).Text) + 1).ToString();
				}
			}
			SetTabIndexes();
		}

		private void RemoveRow(int rowIndex)
		{
			RequestSuspendLayout();

			for(int col = 0; col < this.ColumnCount; col++)
			{
				Control control = this.GetControlFromPosition(col, rowIndex);
				this.Controls.Remove(control);
			}
			foreach(Control control in this.Controls)
			{
				int controlRow = this.GetRow(control);
				if(controlRow < rowIndex)
					continue;

				this.SetRow(control, controlRow - 1);

				if(control.Name == "RowNumberTextBox")
				{
					(control as TextBox).Text = (Int32.Parse((control as TextBox).Text) - 1).ToString();
				}
			}
			this.RowCount--;
			SetTabIndexes();

			RequestResumeLayout();
		}

		private void MoveRow(int fromRow, int toRow)
		{
			if(fromRow == toRow)
				return;
			Task task = project.MoveRow(fromRow, toRow, showActive);
			RemoveRow(fromRow);
			//going down, push toRow up - already done by removing task
			//going up, push toRow down
			InsertTaskRowAt(toRow, task);
			FocusOnTitle(fromRow);
			Control control = this.GetControlFromPosition(column: TITLE_COLUMN_INDEX, row: toRow);
/*			if(control is TitleTextBox)
			{
				(control as TitleTextBox).SetTextBoxHeightByText();
			}*/
		}

		private void FocusOnTitle(int row, int caret = -1, int selectionLength = 0)
		{
			Control control = this.GetControlFromPosition(TITLE_COLUMN_INDEX, row);
			if(control == null || !(control is RichTextBox))
				return;

			control.Focus();
			if(control is RichTextBox)
			{
				RichTextBox textBox = (control as RichTextBox);
				if(caret == -1)
				{
					caret = 0;
					selectionLength = 0;
				}
				textBox.Select(caret, selectionLength);
			}
		}

		private void SelectTitleTextBox(int fromRow, int toRow)
		{
			if(toRow <= 0)
				return;
			RichTextBox previousTextBox = (RichTextBox)this.GetControlFromPosition(column: TITLE_COLUMN_INDEX, row: fromRow);
			int caret = previousTextBox.SelectionStart;
			if(fromRow < toRow)
			{
				caret = 0;
			}
			FocusOnTitle(toRow, caret);
		}

		private void addTask_Click(object sender, EventArgs e)
		{
			//add task below current
			int row = this.GetRow(sender as Control) + 1;
			InsertTaskRowAt(row, project.InsertNewTask(row, active: showActive));
			history.Add(new AddAction(showActive, row));
			FocusOnTitle(row);
		}

		public void ManualAddTask(bool activeSheet, int row, Task task = null)
		{
			history.Off();
			ToolStrip.SelectActiveInactive(activeSheet);
			if(task == null)
				task = project.InsertNewTask(row, active: showActive);
			else
				project.InsertTask(row, active: showActive, task: task);
			InsertTaskRowAt(row, task);
			history.On();
		}

		private void deleteTask_Click(object sender, EventArgs e)
		{
			int row = this.GetRow(sender as Control);
			Task task = project.GetTask(row, active: showActive);
			project.RemoveTask(row, active: showActive);
			RemoveRow(row);
			history.Add(new DeleteAction(showActive, row, task));
		}

		public void ManualDeleteTask(bool activeSheet, int row)
		{
			history.Off();
			ToolStrip.SelectActiveInactive(activeSheet);
			project.RemoveTask(row, active: showActive);
			RemoveRow(row);
			history.On();
		}

		private void rowNumberTextBox_LostFocus(object sender, EventArgs e)
		{
			TextBox textBox = (sender as TextBox);
			int row = this.GetRow(textBox);
			int newRow;
			if(!Int32.TryParse(textBox.Text, out newRow))
			{
				textBox.Text = row.ToString();
				return;
			}
			MoveRow(row, newRow);
			history.Add(new MoveAction(showActive, row, newRow));
		}

		private void rowNumberTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Enter)
			{
				e.Handled = true;
				e.SuppressKeyPress = true; //stop the error-ding from sounding
			}
		}

		private void rowNumberTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			TextBox textBox = (sender as TextBox);
			int row = this.GetRow(textBox);

			if(e.KeyCode == Keys.Enter)
			{
				this.GetControlFromPosition(column: TITLE_COLUMN_INDEX, row: row).Focus(); //lose focus here to trigger move event
				e.Handled = true;
			}
		}

		private void titleTextBox_GotFocus(object sender, EventArgs e)
		{
			int row = this.GetRow(sender as Control);
			if(row == 1)
			{
				this.ScrollControlIntoView(this.GetControlFromPosition(column: PLUS_COLUMN_INDEX, row: 0));
			}
			else
			{
				this.ScrollControlIntoView(sender as Control);
			}
		}

		private void titleTextBox_TextChanged(object sender, EventArgs e)
		{
			RichTextBox textBox = (sender as RichTextBox);
			int row = this.GetRow(textBox);
			string previousText = project.GetTitle(row, showActive);
			project.UpdateTitle(row, textBox.Text, active: showActive);
			history.Add(new TextAction(showActive, row, previousText, textBox.Text));
		}

		private void titleTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			TitleTextBox textBox = (sender as TitleTextBox);
			if(e.KeyCode == Keys.Down)
			{
				if(e.Control)
				{
					//move to beginning of next textbox
					int row = this.GetRow(sender as Control);
					FocusOnTitle(row + 1);
					e.Handled = true;
				}
				else
				{
					//if cursor is on last line, move to next textbox
					if(textBox.CursorOnLastLine())
					{
						int row = this.GetRow(sender as Control);
						SelectTitleTextBox(row, row + 1);
						e.Handled = true;
					}
				}
			}
			if(e.KeyCode == Keys.Up)
			{
				if(e.Control)
				{
					//move to beginning of next textbox
					int row = this.GetRow(sender as Control);
					FocusOnTitle(row - 1);
					e.Handled = true;
				}
				else
				{
					//if cursor is on first line, move to previous textbox
					if(textBox.CursorOnFirstLine())
					{
						int row = this.GetRow(sender as Control);
						SelectTitleTextBox(row, row - 1);
						e.Handled = true;
					}
				}
			}
		}

		private void titleTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if(e.Control && e.KeyCode == Keys.N)
			{
				addTask_Click(sender, e);
			}
		}

		public void ManualMoveTask(bool activeSheet, int fromRowNumber, int toRowNumber)
		{
			history.Off();
			ToolStrip.SelectActiveInactive(activeSheet);
			MoveRow(fromRowNumber, toRowNumber);
			history.On();
		}

		public void ManualTextChange(bool activeSheet, int row, string text, int caret, int selectionLength)
		{
			history.Off();
			ToolStrip.SelectActiveInactive(activeSheet);
			Control control = this.GetControlFromPosition(TITLE_COLUMN_INDEX, row);
			(control as RichTextBox).Text = text;
			FocusOnTitle(row, caret, selectionLength);
			history.On();
		}

		private void statusComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBox comboBox = (sender as ComboBox);
			int row = this.GetRow(comboBox);
			string previousStatus = project.GetStatus(row, active: showActive);
			if(previousStatus == comboBox.Text)
				return;			

			ChangeStatusAction historyAction = new ChangeStatusAction(showActive, row, previousStatus);
			StatusChangeResult result = project.UpdateStatus(row, comboBox.Text, active: showActive);
			Label dateDoneLabel = (Label)this.GetControlFromPosition(7, row);
			dateDoneLabel.Text = result.DoneDateString;

			if(result.ActiveInactiveChanged)
			{
				historyAction.SetNew(!showActive, 1, comboBox.Text);
				RemoveRow(row);
			}
			else
			{
				historyAction.SetNew(showActive, row, comboBox.Text);
			}
			history.Add(historyAction);
			FocusOnTitle(row);
		}

		private void categoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBox comboBox = (sender as ComboBox);
			int row = this.GetRow(comboBox);
			string previousCategory = project.GetCategory(row, active: showActive);
			if(previousCategory == comboBox.Text)
				return;

			project.UpdateCategory(row, comboBox.Text, active: showActive);
			history.Add(new ChangeCategoryAction(showActive, row, previousCategory, comboBox.Text));
			FocusOnTitle(row);
		}

		private void comboBox_MouseWheel(object sender, MouseEventArgs e)
		{
			(e as HandledMouseEventArgs).Handled = true;
		}

		public void ManualChangeTaskCategory(bool activeSheet, int row, string category)
		{
			history.Off();
			ToolStrip.SelectActiveInactive(activeSheet);
			ComboBox comboBox = this.GetControlFromPosition(CATEGORY_COLUMN_INDEX, row) as ComboBox;
			if(!comboBox.Items.Contains(category))
				comboBox.Items.Add(category);
			comboBox.SelectedIndex = comboBox.Items.IndexOf(category);
			history.On();
		}

		public void ManualChangeTaskStatus(bool currentActiveSheet, int currentRow, bool finalActiveSheet, int finalRow, string status)
		{
			RequestSuspendLayout();
			history.Off();
			ToolStrip.SelectActiveInactive(currentActiveSheet);
			ComboBox comboBox = this.GetControlFromPosition(STATUS_COLUMN_INDEX, currentRow) as ComboBox;
			if(!comboBox.Items.Contains(status))
				comboBox.Items.Add(status);
			comboBox.SelectedIndex = comboBox.Items.IndexOf(status);

			if(currentActiveSheet != finalActiveSheet || currentRow != finalRow)
			{
				ToolStrip.SelectActiveInactive(finalActiveSheet);
				MoveRow(1, finalRow);
			}

			history.On();
			RequestResumeLayout();
		}

		private Label NewDataLabel(string name, string text)
		{
			Label label = new Label();
			label.AutoSize = true;
			label.Font = Settings.REGULAR_FONT;
			label.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
			label.Size = new System.Drawing.Size(21, 24);
			label.Name = name;
			label.Text = text;
			return label;
		}

		private TextBox NewTextBox(string name, string text = null)
		{
			TextBox textBox = new TextBox();
			textBox.Dock = System.Windows.Forms.DockStyle.Top;
			textBox.Font = Settings.REGULAR_FONT;
			textBox.Name = name;
			textBox.Text = text;
			textBox.Size = new System.Drawing.Size(119, 22);
			return textBox;
		}

		private Button NewButton(string text, EventHandler onClickHandler)
		{
			Button button = new Button();
			button.Font = Settings.REGULAR_FONT;
			button.Location = new System.Drawing.Point(3, 3);
			button.AutoSize = true;
			button.TabStop = false;
			button.Text = text;
			button.UseVisualStyleBackColor = true;
			button.Margin = new Padding(0);
			button.Click += onClickHandler;
			return button;
		}

		private void SetTabIndexes()
		{
			for(int row = 1; row < this.RowCount; row++)
			{
				Control titleControl = this.GetControlFromPosition(TITLE_COLUMN_INDEX, row);
				if(titleControl == null)
					continue;
				titleControl.TabIndex = (row*10) + 1;

				Control statusControl = this.GetControlFromPosition(STATUS_COLUMN_INDEX, row);
				statusControl.TabIndex = (row*10) + 2;

				Control categoryControl = this.GetControlFromPosition(CATEGORY_COLUMN_INDEX, row);
				categoryControl.TabIndex = (row*10) + 3;
			}
		}

		public void ClearAllInactive()
		{
			if(showActive)
			{
				MessageBox.Show("Cannot delete inactive items while active items are displayed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			MultipleAction multipleAction = new MultipleAction();
			int row = 1;
			while(this.RowCount > 1)
			{
				Task task = project.GetTask(row, active: showActive);
				project.RemoveTask(row, active: showActive);
				RemoveRow(row);
				multipleAction.AddAction(new DeleteAction(showActive, row, task));
			}
			history.Add(multipleAction);
		}
	}
}
