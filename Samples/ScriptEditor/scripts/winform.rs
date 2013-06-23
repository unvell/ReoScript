// ReoScript Sample
// 
// Demo Points:
//   - Use 'import' keyword import .Net namespaces or types
//   - Self-adaptive type conversion
//   - Constructor and initializer
//   - Event binding
//	 - setTimeout thread-safety
// 
// copyright (c) 2013 unvell all rights reserved

import System.Windows.Forms.*;
import System.Drawing.*;

var f = new Form() {
  text: 'ReoScript Form', startPosition: 'CenterScreen', 
  resize: function() { link.location = { x: (this.width - link.width) / 2, y : link.top }; },
  load: function() { updateLabelText(); }
};

var link = new LinkLabel() {
  text: 'click me to close window', autoSize: true, location: { x: 75, y: 100 },
  click: function() { f.close(); f = null; },
};

var stateCount = 10, stateIndex = 0;

var lab = new Label() {
  text: '='.repeat(stateCount), location: { x: 100, y: 50 }, autoSize: true,
};

function updateLabelText() {
  lab.text = '='.repeat(stateIndex) + '>' + '='.repeat(stateCount - stateIndex);
  if(stateIndex<stateCount) stateIndex++; else stateIndex = 0;

  if(f != null) setTimeout(updateLabelText, 100);
}

f.controls.addRange([link, lab]);
f.showDialog();
