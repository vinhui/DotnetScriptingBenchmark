(module
 (type $0 (func (param i32 i32) (result i32)))
 (type $1 (func (param i32) (result i32)))
 (type $2 (func))
 (import "" "addCs" (func $assembly/index/addCs (type $0) (param i32 i32) (result i32)))
 (memory $0 0)
 (export "add" (func $assembly/index/add))
 (export "addLoop" (func $assembly/index/addLoop))
 (export "addLoopInterop" (func $assembly/index/addLoopInterop))
 (export "memory" (memory $0))
 (export "_start" (func $~start))
 (func $assembly/index/add (type $0) (param $0 i32) (param $1 i32) (result i32)
  local.get $0
  local.get $1
  i32.add
 )
 (func $assembly/index/addLoop (type $1) (param $0 i32) (result i32)
  (local $1 i32)
  (local $2 i32)
  loop $for-loop|0
   local.get $0
   local.get $1
   i32.gt_s
   if
    local.get $2
    local.get $1
    local.get $1
    i32.add
    i32.add
    local.set $2
    local.get $1
    i32.const 1
    i32.add
    local.set $1
    br $for-loop|0
   end
  end
  local.get $2
 )
 (func $assembly/index/addLoopInterop (type $1) (param $0 i32) (result i32)
  (local $1 i32)
  (local $2 i32)
  loop $for-loop|0
   local.get $0
   local.get $1
   i32.gt_s
   if
    local.get $1
    local.get $1
    call $assembly/index/addCs
    local.get $2
    i32.add
    local.set $2
    local.get $1
    i32.const 1
    i32.add
    local.set $1
    br $for-loop|0
   end
  end
  local.get $2
 )
 (func $~start (type $2)
 )
)
