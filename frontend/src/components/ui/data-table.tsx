import { useEffect, useMemo, useState } from "react";
import { Pencil, Trash2, ChevronUp, ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { cn } from "@/utils/cn";

export interface DataTableColumn<T> {
  key: string;
  header: string;
  render: (row: T) => React.ReactNode;
  sortable?: boolean;
  width?: string;
  align?: "left" | "center" | "right";
}

type SortDirection = "asc" | "desc" | null;

interface SortState {
  key: string;
  direction: SortDirection;
}

export function DataTable<T>({
  columns,
  rows,
  getRowKey,
  onEdit,
  onDelete,
  pageSize = 10,
  className
}: {
  columns: DataTableColumn<T>[];
  rows: T[];
  getRowKey: (row: T) => string;
  onEdit?: (row: T) => void;
  onDelete?: (row: T) => void;
  pageSize?: number;
  className?: string;
}) {
  const hasActions = Boolean(onEdit || onDelete);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState<SortState>({ key: "", direction: null });

  const totalPages = Math.max(1, Math.ceil(rows.length / pageSize));

  useEffect(() => {
    setPage(1);
  }, [rows.length, pageSize]);

  useEffect(() => {
    setPage((p) => Math.min(p, totalPages));
  }, [totalPages]);

  const sortedRows = useMemo(() => {
    if (!sort.direction) return rows;

    return [...rows].sort((a, b) => {
      const aValue = getColumnValue(a, sort.key);
      const bValue = getColumnValue(b, sort.key);

      if (aValue === null || aValue === undefined) return 1;
      if (bValue === null || bValue === undefined) return -1;

      const comparison = aValue < bValue ? -1 : aValue > bValue ? 1 : 0;
      return sort.direction === "asc" ? comparison : -comparison;
    });
  }, [rows, sort]);

  const pagedRows = useMemo(() => {
    const start = (page - 1) * pageSize;
    const end = start + pageSize;
    return sortedRows.slice(start, end);
  }, [sortedRows, page, pageSize]);

  const showingFrom = rows.length === 0 ? 0 : (page - 1) * pageSize + 1;
  const showingTo = Math.min(page * pageSize, rows.length);
  const showPagination = rows.length > pageSize;

  const handleSort = (columnKey: string) => {
    const column = columns.find(col => col.key === columnKey);
    if (!column?.sortable) return;

    setSort(current => {
      if (current.key !== columnKey) {
        return { key: columnKey, direction: "asc" };
      }
      if (current.direction === "asc") {
        return { key: columnKey, direction: "desc" };
      }
      return { key: columnKey, direction: null };
    });
  };

  const getColumnValue = (row: T, key: string): any => {
    // This is a simplified approach - in a real app, you might want to pass a getter function
    return (row as any)[key];
  };

  const getSortIcon = (columnKey: string) => {
    if (sort.key !== columnKey || !sort.direction) return null;
    return sort.direction === "asc" ? (
      <ChevronUp className="h-3.5 w-3.5" />
    ) : (
      <ChevronDown className="h-3.5 w-3.5" />
    );
  };

  return (
    <Card className={cn("overflow-hidden", className)}>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-200">
          <thead className="bg-slate-50">
            <tr>
              {columns.map((column) => (
                <th
                  key={column.key}
                  className={cn(
                    "px-5 py-3.5 text-xs font-semibold uppercase tracking-[0.22em] text-slate-500 transition-colors",
                    column.sortable && "cursor-pointer hover:bg-slate-100 hover:text-slate-700",
                    column.align === "center" && "text-center",
                    column.align === "right" && "text-right"
                  )}
                  style={{ width: column.width }}
                  onClick={() => column.sortable && handleSort(column.key)}
                >
                  <div className={cn(
                    "flex items-center gap-1",
                    column.align === "center" && "justify-center",
                    column.align === "right" && "justify-end"
                  )}>
                    {column.header}
                    {getSortIcon(column.key)}
                  </div>
                </th>
              ))}
              {hasActions ? (
                <th className="px-5 py-3.5 text-right text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">
                  Actions
                </th>
              ) : null}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 bg-white">
            {pagedRows.map((row, index) => (
              <tr
                key={getRowKey(row)}
                className={cn(
                  "align-middle transition-all duration-150",
                  "hover:bg-slate-50/70",
                  index % 2 === 0 && "bg-white",
                  index % 2 === 1 && "bg-slate-50/30"
                )}
              >
                {columns.map((column) => (
                  <td 
                    key={column.key} 
                    className={cn(
                      "px-5 py-3.5 text-sm text-slate-700",
                      column.align === "center" && "text-center",
                      column.align === "right" && "text-right"
                    )}
                  >
                    {column.render(row)}
                  </td>
                ))}
                {hasActions ? (
                  <td className="px-5 py-3.5">
                    <div className="flex items-center justify-end gap-2">
                      {onEdit ? (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => onEdit(row)}
                          className="hover:bg-brand-50 hover:border-brand-200 hover:text-brand-700"
                        >
                          <Pencil className="h-3.5 w-3.5" />
                          Edit
                        </Button>
                      ) : null}
                      {onDelete ? (
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-9 px-3 text-sm text-rose-600 hover:bg-rose-50 hover:text-rose-700 focus-visible:ring-rose-400"
                          onClick={() => onDelete(row)}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                          Delete
                        </Button>
                      ) : null}
                    </div>
                  </td>
                ) : null}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {showPagination ? (
        <div className="flex items-center justify-between gap-4 border-t border-slate-100 px-5 py-3 text-xs text-slate-500">
          <span className="flex items-center gap-1">
            Showing <span className="font-medium text-slate-700">{showingFrom}</span>
            to
            <span className="font-medium text-slate-700">{showingTo}</span>
            of
            <span className="font-medium text-slate-700">{rows.length}</span>
            results
          </span>
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              className="h-9 px-3 disabled:opacity-50"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
            >
              Previous
            </Button>
            <span className="flex items-center gap-1 px-3 py-2 text-sm font-medium text-slate-700 bg-slate-100 rounded-lg">
              Page {page} of {totalPages}
            </span>
            <Button
              variant="ghost"
              size="sm"
              className="h-9 px-3 disabled:opacity-50"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            >
              Next
            </Button>
          </div>
        </div>
      ) : (
        <div className="border-t border-slate-100 px-5 py-3 text-xs text-slate-400">
          {rows.length} {rows.length === 1 ? "record" : "records"}
        </div>
      )}
    </Card>
  );
}
