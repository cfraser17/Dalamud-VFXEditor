using System;
using System.Collections.Generic;

namespace VfxEditor.AvfxFormat {
    public class UiNodeGraphItem {
        public int Level;
        public int Level2;
        public List<AvfxNode> Next;
    }

    public class UiNodeGraph {
        public Dictionary<AvfxNode, UiNodeGraphItem> Graph = new();
        public bool Outdated = false;
        public bool Cycle = false;

        public UiNodeGraph( AvfxNode node ) {
            ParseGraph( 0, node, new() );
            var level2Dict = new Dictionary<int, int>();
            foreach( var val in Graph.Values ) {
                if( level2Dict.ContainsKey( val.Level ) ) {
                    level2Dict[val.Level] += 1;
                    val.Level2 = level2Dict[val.Level];
                }
                else {
                    level2Dict[val.Level] = 0;
                    val.Level2 = 0;
                }
            }
        }

        public void ParseGraph( int level, AvfxNode node, HashSet<AvfxNode> visited ) {
            if( visited.Contains( node ) || Cycle ) {
                Cycle = true;
                return;
            }
            if( Graph.TryGetValue( node, out var value ) ) { // already defined
                if( level > value.Level ) {
                    PushBack( node, level - value.Level );
                }

                value.Level = Math.Max( level, value.Level );
            }
            else {
                visited.Add( node );
                var item = new UiNodeGraphItem {
                    Level = level,
                    Next = new()
                };
                foreach( var n in node.Parents ) {
                    item.Next.Add( n.Node );
                    ParseGraph( level + 1, n.Node, new HashSet<AvfxNode>( visited ) );
                }
                Graph[node] = item;
            }
        }

        public void PushBack( AvfxNode node, int amount ) {
            Graph[node].Level += amount;
            foreach( var item in Graph[node].Next ) {
                PushBack( item, amount );
            }
        }

        public void NowOutdated() {
            Outdated = true;
        }
    }
}
