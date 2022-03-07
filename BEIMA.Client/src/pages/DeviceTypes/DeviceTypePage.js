import { useState, useEffect } from "react"
import {ItemCard} from "../../shared/ItemCard/ItemCard"
import styles from './DeviceTypePage.module.css'
import { useOutletContext } from 'react-router-dom';
import { Form, Card, Button, FormControl} from "react-bootstrap";
import { IoAdd } from "react-icons/io5";
import { v4 as uuidv4 } from 'uuid';

const DeviceTypePage = () => {
  const [deviceType, setDeviceType] = useState(null)
  const [loading, setLoading] = useState(true)
  const [setPageName] = useOutletContext();

  useEffect(() => {
    setPageName('View Device Type')
  },[setPageName])

  const mockCall = async () => {
    const sleep = ms => new Promise(resolve => setTimeout(resolve, ms))
    await sleep(1000)
    let data = {
      deviceTypeId: 54,
      name: `Batteries`,
      description: "Battery devices are used to store power that other devices can use.",
      notes: "There are no notes",
      fields: {
        fieldIdOne: "Dimensions",
        fieldIdTwo: "Weight",
        fieldIdThree: "Color",
        fieldIdFour: "Manufactorer"
      }
    }
    
    // Map data into format supported by list
    let mapped = {
      id: data.deviceTypeId,
      name: data.name,
      description: data.description,
      notes: data.notes,
      fields: data.fields,
      addedFields:{},
      deletedFields:[]
    }

    return mapped
  }

  useEffect(() => {
    const loadData = async () => {
      setLoading(true)
      let type = await mockCall()
      setDeviceType(type)
      setLoading(false)
    }
    loadData()
  },[])

  /**
   * Renders a card styled field in a device type.
   * Allows the user to update the input inside of it
   * as well as delete the field in the higher level <DeviceTypePage/>
   * @param field: field uid
   * @param value: field value
   * @param editable: can this input be modified
   * @param deleteField: function to delete field off the device type
   * in higher level <DeviceTypePage/>
   * @returns 
   */
  const Field = ({field, value, editable, fieldChange, deleteField}) => {
    return (
      <Card>
        <Card.Body >
            <Form.Group className="mb-3" controlId={field}>
              <Form.Label>Field Name</Form.Label>
              <FormControl required type="text" disabled={!editable} size="sm" placeholder="Field Name" value={value} onChange={fieldChange}/>
            </Form.Group>                
          { editable ? 
           <div className={styles.deleteButton}>
              <Button variant="danger" onClick={() => deleteField(field)}>
                Delete
              </Button>
            </div>
          : null}
        </Card.Body>
      </Card>
    )
  }  

  /**
   * Renders a custom form that enables a user
   * to update a device's type's fields
   * 
   * @param item: json device type
   * @param setitem: function to update item in higher level <DeviceTypePage/>
   * @returns 
   */
  const RenderItem = ({item, setItem}) => {
    const [editable, setEditable] = useState(false)
    
    const [description, setDescription] = useState(item.description)
    const [notes, setNotes] = useState(item.notes)
    const [fields, setFields] = useState({...item.fields})
    const [addedFields, setAddedFields] = useState({...item.addedFields})
    const [deletedFields, setDeletedFields] = useState([...item.deletedFields])
    

    const handleSubmit = () => {
      const result = {
        description: description,
        notes: notes,
        fields: fields,
        addedFields: addedFields,
        deletedFields: deletedFields
      }
      //call endpoint
      setItem(result)
      console.log(result)
    }

    const addField = () => {
      const key = uuidv4()
      let temp = {...addedFields}
      temp[key] = ""
      setAddedFields(temp)
    }

    const deleteField = (field) => {
      let tempF = {...fields}
      let tempAf = {...addedFields}
      let tempDf = [...deletedFields]

      if(field in tempF){
        delete tempF[field]
        tempDf.push(field)
        setFields(tempF)
        setDeletedFields(tempDf)
      } else {
        delete tempAf[field]
        setAddedFields(tempAf)
      }
    }

    const onDescriptionChange = (event) => {
      setDescription(event.target.value)
    }

    const onNotesChange = (event) => {
      setNotes(event.target.value)
    }

    const onFieldChange = (event) => {
      let id = event.target.id
      let temp = {...fields}
      temp[id] = event.target.value
      setFields(temp)
    }

    const onAddedFieldChange = (event) => {
      let id = event.target.id
      let temp = {...addedFields}
      temp[id] = event.target.value
      setAddedFields(temp)
    }

    const cancel = () => {
      setEditable(false)
      setDescription(item.description)
      setNotes(item.notes)
      setFields({...item.fields})
      setAddedFields({...item.addedFields})
      setDeletedFields([...item.deletedFields])
    }

    return (
      <Form className={styles.form}>
        <Form.Group className="mb-3">
          {editable ? 
           <div className={styles.buttonRow}>
              <Button id="savebtn" onClick={handleSubmit}>
                Save
              </Button>
              <Button variant="secondary" id="cancelbtn" onClick={cancel}>
                Cancel
              </Button>
           </div>
          : 
            <Button variant="primary" id="editbtn" onClick={() => setEditable(true)}>
              Edit
            </Button>
          }
        </Form.Group>

        <Form.Group className="mb-3" controlId="description">
          <Form.Label><b>Description</b></Form.Label>
          <Form.Control required as="textarea" rows={3} placeholder="Device Type Description"  disabled={!editable} value={description} onChange={onDescriptionChange}/>
        </Form.Group>

        <Form.Group className="mb-3" controlId="notes">
          <Form.Label><b>Notes</b></Form.Label>
          <Form.Control required as="textarea" rows={1} placeholder="Device Type Notes"  disabled={!editable} value={notes}  onChange={onNotesChange}/>
        </Form.Group>

        <Form.Group  className="mb-3">
          <Form.Label><b>Manditory Fields</b></Form.Label>
          <div className={styles.fields}>
            <Field field="manId" value="Manufacturer" editable={false}/>
            <Field field="modId" value="Model Number" editable={false}/>
            <Field field="serId" value="Serial Number" editable={false}/>
            <Field field="yearId" value="Year Manufactured" editable={false}/>
          </div>
        </Form.Group>
        
        <Form.Group  className={[styles.between, "mb-3" ].join(' ')}>
          <Form.Label><b>Additional Fields</b></Form.Label>
          {editable ? 
            <Button variant="primary" id="addbtn" onClick={addField} >
             <IoAdd size={20} color="#fff"/>
            </Button>
          : null}
        </Form.Group>
        <Form.Group id="additionalfields">
          <div className={styles.fields}>
            {Object.keys(fields).map((field, i) => <Field key={i} field={field} editable={editable} value={fields[field]} fieldChange={onFieldChange} deleteField={deleteField}/>)}
            {Object.keys(addedFields).map((field, i) => <Field key={i} field={field} editable={editable} value={addedFields[field]} fieldChange={onAddedFieldChange} deleteField={deleteField}/>)}
          </div>  
        </Form.Group>
      </Form>
    )
  }

  return (
    <div className={styles.item} id="deviceTypeContent">
      <ItemCard 
        title={loading ? 'Loading' : deviceType.name}
        RenderItem={<RenderItem item={deviceType} setItem={setDeviceType}/>} 
        loading={loading}
        route="/deviceTypes"
      />
    </div>
  )
}

export default DeviceTypePage