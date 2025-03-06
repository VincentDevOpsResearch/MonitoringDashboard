import React, { useState } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  TablePagination,
  TextField,
  TableSortLabel,
  Box,
} from '@mui/material';
import { visuallyHidden } from '@mui/utils';

interface QueueData {
  virtualHost: string;
  name: string;
  type: string;
  state: string;
  readyMessages: number;
  unackedMessages: number;
  totalMessages: number;
  incomingRate: number;
  unackedRate: number;
}

interface MessageQueueTableProps {
  data: QueueData[];
}

interface Column {
  id: keyof QueueData;
  label: string;
  minWidth?: number;
  align?: 'right';
}

const columns: Column[] = [
  { id: 'virtualHost', label: 'Virtual Host', minWidth: 150 },
  { id: 'name', label: 'Queue Name', minWidth: 150 },
  { id: 'type', label: 'Type', minWidth: 100 },
  { id: 'state', label: 'State', minWidth: 150 },
  { id: 'readyMessages', label: 'Ready Messages', minWidth: 150, align: 'right' },
  { id: 'unackedMessages', label: 'Unacked Messages', minWidth: 150, align: 'right' },
  { id: 'totalMessages', label: 'Total Messages', minWidth: 150, align: 'right' },
  { id: 'incomingRate', label: 'Incoming Rate', minWidth: 150, align: 'right' },
  { id: 'unackedRate', label: 'Unacked Rate', minWidth: 150, align: 'right' },
];

// State color mapping
const stateColor = (state: string) => {
  switch (state) {
    case 'running':
      return 'green';
    case 'flow':
      return 'orange';
    case 'idle':
      return 'blue';
    case 'blocked':
      return 'red';
    case 'unblocked':
      return 'purple';
    default:
      return 'gray';
  }
};

const MessageQueueTable: React.FC<MessageQueueTableProps> = ({ data }) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchText, setSearchText] = useState('');
  const [order, setOrder] = useState<'asc' | 'desc'>('asc');
  const [orderBy, setOrderBy] = useState<keyof QueueData>('name');

  const handleRequestSort = (property: keyof QueueData) => {
    const isAsc = orderBy === property && order === 'asc';
    setOrder(isAsc ? 'desc' : 'asc');
    setOrderBy(property);
  };

  const handleSearch = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchText(event.target.value);
  };

  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(+event.target.value);
    setPage(0);
  };

  const filteredData = data.filter((row) =>
    Object.keys(row).some((key) =>
      row[key as keyof QueueData].toString().toLowerCase().includes(searchText.toLowerCase())
    )
  );

  const sortedData = filteredData.sort((a, b) => {
    if (orderBy && order) {
      const isAsc = order === 'asc';
      return (a[orderBy] < b[orderBy] ? -1 : 1) * (isAsc ? 1 : -1);
    }
    return 0;
  });

  return (
    <Paper sx={{ width: '100%', overflow: 'hidden' }}>
      <TextField
        value={searchText}
        onChange={handleSearch}
        placeholder="Search..."
        variant="outlined"
        fullWidth
        margin="normal"
      />
      <TableContainer>
        <Table stickyHeader aria-label="sticky table">
          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell
                  key={column.id}
                  align={column.align}
                  sortDirection={orderBy === column.id ? order : false}
                >
                  <TableSortLabel
                    active={orderBy === column.id}
                    direction={orderBy === column.id ? order : 'asc'}
                    onClick={() => handleRequestSort(column.id)}
                  >
                    {column.label}
                    {orderBy === column.id ? (
                      <Box component="span" sx={visuallyHidden}>
                        {order === 'desc' ? 'sorted descending' : 'sorted ascending'}
                      </Box>
                    ) : null}
                  </TableSortLabel>
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {sortedData.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage).map((row, index) => (
              <TableRow hover role="checkbox" tabIndex={-1} key={index}>
                {columns.map((column) => {
                  const value = row[column.id];
                  return (
                    <TableCell key={column.id} align={column.align}>
                      {column.id === 'state' ? (
                        <div style={{ display: 'flex', alignItems: 'center' }}>
                          <div
                            style={{
                              width: 10,
                              height: 10,
                              backgroundColor: stateColor(value as string),
                              marginRight: 8,
                              borderRadius: 2,
                            }}
                          ></div>
                          {value}
                        </div>
                      ) : (
                        value
                      )}
                    </TableCell>
                  );
                })}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        rowsPerPageOptions={[10, 25, 50]}
        component="div"
        count={sortedData.length}
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
      />
    </Paper>
  );
};

export default MessageQueueTable;
