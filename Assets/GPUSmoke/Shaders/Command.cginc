struct IndirectDispatchCmd {
    uint x, y, z;
};

struct IndirectDrawCmd {
    uint index_count_per_instance;
    uint instance_count;
    uint start_index;
    uint base_vertex;
    uint start_instance;
};