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
import Dagre from '@dagrejs/dagre';
import { dependenciesApi } from '../api/services';
import { getErrorMessage } from '../api/client';
import type { DependencyGraphNode, DependencyGraphEdge } from '../api/types';
import { Skeleton } from '@/components/ui/skeleton';

function getLayoutedElements(nodes: Node[], edges: Edge[]): Node[] {
  const g = new Dagre.graphlib.Graph().setDefaultEdgeLabel(() => ({}));
  g.setGraph({ rankdir: 'TB', nodesep: 80, ranksep: 100 });

  nodes.forEach((node) => g.setNode(node.id, { width: 180, height: 60 }));
  edges.forEach((edge) => g.setEdge(edge.source, edge.target));

  Dagre.layout(g);

  return nodes.map((node) => {
    const pos = g.node(node.id);
    return { ...node, position: { x: pos.x - 90, y: pos.y - 30 } };
  });
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
      if (connection.source && connection.target) {
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
