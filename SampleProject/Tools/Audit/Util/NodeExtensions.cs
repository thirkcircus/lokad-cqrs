#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System.Windows.Forms;

namespace Audit.Util
{
    public static class NodeExtensions
    {
        public static TreeNode AddNode(this TreeNode node, string name, object tag = null, string image = "")
        {
            var child = node.Nodes.Add(name, name);
            if (!string.IsNullOrEmpty(image))
            {
                child.SelectedImageKey = child.ImageKey = image;
            }

            child.Tag = tag;
            return child;
        }

        public static TreeNode AddNode(this TreeView view, string name, object tag = null, string image = "")
        {
            var child = view.Nodes.Add(name, name);
            if (!string.IsNullOrEmpty(image))
            {
                child.SelectedImageKey = child.ImageKey = image;
            }

            child.Tag = tag;
            return child;
        }
    }
}