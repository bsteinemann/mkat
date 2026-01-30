import { useCallback, useEffect, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { toast } from 'sonner';
import {
  ReactFlow,
  Background,
  Controls,
  useNodesState,
  useEdgesState,
  Handle,
  Position,
  type Node,
  type Edge,
  type Connection,
  type NodeMouseHandler,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { dependenciesApi } from '../api/services';
import { getErrorMessage } from '../api/client';
import type { DependencyGraphNode, DependencyGraphEdge } from '../api/types';
import { Skeleton } from '@/components/ui/skeleton';

const NODE_WIDTH = 180;
const NODE_HEIGHT = 60;
const NODE_SEP = 80;
const RANK_SEP = 100;

function getLayoutedElements(nodes: Node[], edges: Edge[]): Node[] {
  // Build adjacency: target → sources (which nodes point to it)
  const incomingMap = new Map<string, string[]>();
  const outgoingMap = new Map<string, string[]>();
  for (const node of nodes) {
    incomingMap.set(node.id, []);
    outgoingMap.set(node.id, []);
  }
  for (const edge of edges) {
    incomingMap.get(edge.target)?.push(edge.source);
    outgoingMap.get(edge.source)?.push(edge.target);
  }

  // Topological sort via Kahn's algorithm to assign ranks (layers)
  const inDegree = new Map<string, number>();
  for (const node of nodes) {
    inDegree.set(node.id, incomingMap.get(node.id)?.length ?? 0);
  }
  const queue: string[] = [];
  for (const [id, deg] of inDegree) {
    if (deg === 0) queue.push(id);
  }
  const rank = new Map<string, number>();
  while (queue.length > 0) {
    const id = queue.shift()!;
    const r = Math.max(0, ...(incomingMap.get(id) ?? []).map((src) => (rank.get(src) ?? 0) + 1));
    rank.set(id, r);
    for (const target of outgoingMap.get(id) ?? []) {
      const newDeg = (inDegree.get(target) ?? 1) - 1;
      inDegree.set(target, newDeg);
      if (newDeg === 0) queue.push(target);
    }
  }
  // Assign rank 0 to any unranked nodes (isolated or in cycles)
  for (const node of nodes) {
    if (!rank.has(node.id)) rank.set(node.id, 0);
  }

  // Group nodes by rank
  const layers = new Map<number, string[]>();
  for (const [id, r] of rank) {
    if (!layers.has(r)) layers.set(r, []);
    layers.get(r)!.push(id);
  }

  // Assign positions
  const posMap = new Map<string, { x: number; y: number }>();
  for (const [r, ids] of layers) {
    const totalWidth = ids.length * NODE_WIDTH + (ids.length - 1) * NODE_SEP;
    const startX = -totalWidth / 2;
    ids.forEach((id, i) => {
      posMap.set(id, {
        x: startX + i * (NODE_WIDTH + NODE_SEP),
        y: r * (NODE_HEIGHT + RANK_SEP),
      });
    });
  }

  return nodes.map((node) => ({
    ...node,
    position: posMap.get(node.id) ?? { x: 0, y: 0 },
  }));
}

const stateColors: Record<string, string> = {
  Up: 'bg-green-100 border-green-500 dark:bg-green-900 dark:border-green-600',
  Down: 'bg-red-100 border-red-500 dark:bg-red-900 dark:border-red-600',
  Unknown: 'bg-gray-100 border-gray-400 dark:bg-gray-800 dark:border-gray-600',
  Paused: 'bg-yellow-100 border-yellow-500 dark:bg-yellow-900 dark:border-yellow-600',
};

function ServiceNode({ data }: { data: { label: string; state: string; isSuppressed: boolean } }) {
  return (
    <div
      className={`px-4 py-2 rounded-lg border-2 shadow-sm ${stateColors[data.state] ?? stateColors.Unknown} ${data.isSuppressed ? 'opacity-50' : ''}`}
    >
      <Handle type="target" position={Position.Top} />
      <div className="text-sm font-medium">{data.label}</div>
      <Handle type="source" position={Position.Bottom} />
    </div>
  );
}

const nodeTypes = { serviceNode: ServiceNode };

function toFlowElements(
  graphNodes: DependencyGraphNode[],
  graphEdges: DependencyGraphEdge[],
): { nodes: Node[]; edges: Edge[] } {
  const nodes: Node[] = graphNodes.map((n) => ({
    id: n.id,
    type: 'serviceNode',
    position: { x: 0, y: 0 },
    data: { label: n.name, state: n.state, isSuppressed: n.isSuppressed },
  }));

  const edges: Edge[] = graphEdges.map((e) => ({
    id: `${e.dependentId}-${e.dependencyId}`,
    source: e.dependentId,
    target: e.dependencyId,
    animated: true,
  }));

  const layoutedNodes = getLayoutedElements(nodes, edges);
  return { nodes: layoutedNodes, edges };
}

export function DependencyMap() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const { data, isLoading, error } = useQuery({
    queryKey: ['dependency-graph'],
    queryFn: () => dependenciesApi.graph(),
  });

  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);

  const flowElements = useMemo(() => {
    if (!data) return null;
    return toFlowElements(data.nodes, data.edges);
  }, [data]);

  useEffect(() => {
    if (flowElements) {
      setNodes(flowElements.nodes);
      setEdges(flowElements.edges);
    }
  }, [flowElements, setNodes, setEdges]);

  const addDependency = useMutation({
    mutationFn: ({ source, target }: { source: string; target: string }) =>
      dependenciesApi.add(source, target),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dependency-graph'] });
      toast.success('Dependency added');
    },
    onError: (err) => {
      toast.error(getErrorMessage(err, 'Failed to add dependency'));
    },
  });

  const removeDependency = useMutation({
    mutationFn: ({ source, target }: { source: string; target: string }) =>
      dependenciesApi.remove(source, target),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dependency-graph'] });
      toast.success('Dependency removed');
    },
    onError: (err) => {
      toast.error(getErrorMessage(err, 'Failed to remove dependency'));
    },
  });

  const onConnect = useCallback(
    (connection: Connection) => {
      if (connection.source && connection.target && connection.source !== connection.target) {
        addDependency.mutate({ source: connection.source, target: connection.target });
      }
    },
    [addDependency],
  );

  const onEdgesDelete = useCallback(
    (deletedEdges: Edge[]) => {
      for (const edge of deletedEdges) {
        removeDependency.mutate({ source: edge.source, target: edge.target });
      }
    },
    [removeDependency],
  );

  const onNodeClick: NodeMouseHandler = useCallback(
    (_event, node) => {
      navigate({ to: '/services/$serviceId', params: { serviceId: node.id } });
    },
    [navigate],
  );

  if (isLoading) {
    return (
      <div className="p-6">
        <h1 className="text-2xl font-bold mb-6">Dependency Map</h1>
        <Skeleton className="h-[600px] w-full rounded-lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6">
        <h1 className="text-2xl font-bold mb-6">Dependency Map</h1>
        <p className="text-destructive">Failed to load dependency graph.</p>
      </div>
    );
  }

  return (
    <div className="p-6 flex flex-col h-full">
      <h1 className="text-2xl font-bold mb-4">Dependency Map</h1>
      <p className="text-sm text-muted-foreground mb-4">
        Click a node to view the service. Drag from one handle to another to add a dependency.
        Select an edge and press Delete to remove it.
      </p>
      <div className="flex-1 min-h-[600px] rounded-lg border bg-background">
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onConnect={onConnect}
          onEdgesDelete={onEdgesDelete}
          onNodeClick={onNodeClick}
          nodeTypes={nodeTypes}
          deleteKeyCode="Delete"
          fitView
          fitViewOptions={{ padding: 0.2 }}
        >
          <Background />
          <Controls />
        </ReactFlow>
      </div>
    </div>
  );
}
