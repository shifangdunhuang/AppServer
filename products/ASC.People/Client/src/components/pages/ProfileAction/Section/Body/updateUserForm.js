import React from 'react'
import { withRouter } from 'react-router'
import { connect } from 'react-redux'
import { Avatar, Button, Textarea, Text, toastr, ModalDialog, TextInput } from 'asc-web-components'
import { withTranslation } from 'react-i18next';
import { toEmployeeWrapper, getUserRole, getUserContactsPattern, getUserContacts, mapGroupsToGroupSelectorOptions, mapGroupSelectorOptionsToGroups, filterGroupSelectorOptions } from "../../../../../store/people/selectors";
import { updateProfile } from '../../../../../store/profile/actions';
import { sendInstructionsToChangePassword, sendInstructionsToChangeEmail } from "../../../../../store/services/api";
import { MainContainer, AvatarContainer, MainFieldsContainer } from './FormFields/Form'
import TextField from './FormFields/TextField'
import TextChangeField from './FormFields/TextChangeField'
import DateField from './FormFields/DateField'
import RadioField from './FormFields/RadioField'
import DepartmentField from './FormFields/DepartmentField'
import ContactsField from './FormFields/ContactsField'
import InfoFieldContainer from './FormFields/InfoFieldContainer'
import { departments, department, position, employedSinceDate, typeGuest, typeUser } from '../../../../../helpers/customNames';

class UpdateUserForm extends React.Component {

  constructor(props) {
    super(props);

    this.state = this.mapPropsToState(props);

    this.validate = this.validate.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
    this.onInputChange = this.onInputChange.bind(this);
    this.onUserTypeChange = this.onUserTypeChange.bind(this);
    this.onBirthdayDateChange = this.onBirthdayDateChange.bind(this);
    this.onWorkFromDateChange = this.onWorkFromDateChange.bind(this);
    this.onCancel = this.onCancel.bind(this);

    this.onEmailChange = this.onEmailChange.bind(this);
    this.onSendEmailChangeInstructions = this.onSendEmailChangeInstructions.bind(this);
    this.onPasswordChange = this.onPasswordChange.bind(this);
    this.onSendPasswordChangeInstructions = this.onSendPasswordChangeInstructions.bind(this);
    this.onPhoneChange = this.onPhoneChange.bind(this);
    this.onSendPhoneChangeInstructions = this.onSendPhoneChangeInstructions.bind(this);
    this.onDialogClose = this.onDialogClose.bind(this);

    this.onContactsItemAdd = this.onContactsItemAdd.bind(this);
    this.onContactsItemTypeChange = this.onContactsItemTypeChange.bind(this);
    this.onContactsItemTextChange = this.onContactsItemTextChange.bind(this);

    this.onShowGroupSelector = this.onShowGroupSelector.bind(this);
    this.onCloseGroupSelector = this.onCloseGroupSelector.bind(this);
    this.onSearchGroups = this.onSearchGroups.bind(this);
    this.onSelectGroups = this.onSelectGroups.bind(this);
    this.onRemoveGroup = this.onRemoveGroup.bind(this);
  }

  componentDidUpdate(prevProps, prevState) {
    if (this.props.match.params.userId !== prevProps.match.params.userId) {
      this.setState(this.mapPropsToState(this.props));
    }
  }

  mapPropsToState = (props) => {
    var profile = toEmployeeWrapper(props.profile);
    var allOptions = mapGroupsToGroupSelectorOptions(props.groups);
    var selected = mapGroupsToGroupSelectorOptions(profile.groups); 

    const newState = {
      isLoading: false,
      errors: {
        firstName: false,
        lastName: false,
      },
      profile: profile,
      dialog: {
        visible: false,
        header: "",
        body: "",
        buttons: [],
        newEmail: profile.email,
      },
      selector: {
        visible: false,
        allOptions: allOptions,
        options: [...allOptions],
        selected: selected
      }
    };

    //Set unique contacts id 
    const now = new Date().getTime();

    newState.profile.contacts.forEach((contact, index) => {
      contact.id = (now + index).toString();
    });

    return newState;
  }

  onInputChange(event) {
    var stateCopy = Object.assign({}, this.state);
    stateCopy.profile[event.target.name] = event.target.value;
    this.setState(stateCopy)
  }

  onUserTypeChange(event) {
    var stateCopy = Object.assign({}, this.state);
    stateCopy.profile.isVisitor = event.target.value === "true";
    this.setState(stateCopy)
  }

  onBirthdayDateChange(value) {
    var stateCopy = Object.assign({}, this.state);
    stateCopy.profile.birthday = value ? value.toJSON() : null;
    this.setState(stateCopy)
  }

  onWorkFromDateChange(value) {
    var stateCopy = Object.assign({}, this.state);
    stateCopy.profile.workFrom = value ? value.toJSON() : null;
    this.setState(stateCopy)
  }

  validate() {
    const { profile } = this.state;
    const errors = {
      firstName: !profile.firstName,
      lastName: !profile.lastName,
    };
    const hasError = errors.firstName || errors.lastName;
    this.setState({ errors: errors });
    return !hasError;
  }

  handleSubmit() {
    if(!this.validate())
      return false;

    this.setState({isLoading: true});

    this.props.updateProfile(this.state.profile)
      .then((profile) => {
        toastr.success("Success");
        this.props.history.push(`${this.props.settings.homepage}/view/${profile.userName}`);
      })
      .catch((error) => {
        toastr.error(error.message)
        this.setState({isLoading: false})
      });
  }

  onCancel() {
    this.props.history.goBack();
  }

  onEmailChange(event) {
    const emailRegex = /.+@.+\..+/;
    const newEmail = event.target.value || this.state.dialog.newEmail;
    const hasError = !emailRegex.test(newEmail);

    const dialog = { 
      visible: true,
      header: "Change email",
      body: (
        <Text.Body>
          <span style={{display: "block", marginBottom: "8px"}}>The activation instructions will be sent to the entered email</span>
          <TextInput
            id="new-email"
            scale={true}
            isAutoFocussed={true}
            value={newEmail}
            onChange={this.onEmailChange}
            hasError={hasError}
          />
        </Text.Body>
      ),
      buttons: [
        <Button
          key="SendBtn"
          label="Send"
          size="medium"
          primary={true}
          onClick={this.onSendEmailChangeInstructions}
          isDisabled={hasError}
        />
      ],
      newEmail: newEmail
     };
    this.setState({ dialog: dialog })
  }

  onSendEmailChangeInstructions() {
    sendInstructionsToChangeEmail(this.state.profile.id, this.state.dialog.newEmail)
      .then((res) => {
        res.data.error ? toastr.error(res.data.error.message) : toastr.success(res.data.response)
      })
      .catch((error) => toastr.error(error.message))
      .finally(this.onDialogClose);
  }

  onPasswordChange() {
    const dialog = { 
      visible: true,
      header: "Change password",
      body: (
        <Text.Body>
          Send the password change instructions to the <a href={`mailto:${this.state.profile.email}`}>{this.state.profile.email}</a> email address
        </Text.Body>
      ),
      buttons: [
        <Button
          key="SendBtn"
          label="Send"
          size="medium"
          primary={true}
          onClick={this.onSendPasswordChangeInstructions}
        />
      ]
     };
    this.setState({ dialog: dialog })
  }

  onSendPasswordChangeInstructions() {
    sendInstructionsToChangePassword(this.state.profile.email)
      .then((res) => {
        res.data.error ? toastr.error(res.data.error.message) : toastr.success(res.data.response)
      })
      .catch((error) => toastr.error(error.message))
      .finally(this.onDialogClose);
  }

  onPhoneChange() {
    const dialog = { 
      visible: true,
      header: "Change phone",
      body: (
        <Text.Body>
          The instructions on how to change the user mobile number will be sent to the user email address
        </Text.Body>
      ),
      buttons: [
        <Button
          key="SendBtn"
          label="Send"
          size="medium"
          primary={true}
          onClick={this.onSendPhoneChangeInstructions}
        />
      ]
     };
    this.setState({ dialog: dialog })
  }

  onSendPhoneChangeInstructions() {
    toastr.success("Context action: Change phone");
    this.onDialogClose();
  }

  onDialogClose() {
    const dialog = { visible: false, newEmail: this.state.profile.email }; 
    this.setState({ dialog: dialog })
  }

  onContactsItemAdd(item) {
    var stateCopy = Object.assign({}, this.state);
    stateCopy.profile.contacts.push({
      id: new Date().getTime().toString(),
      type: item.value,
      value: ""
    });
    this.setState(stateCopy);
  }

  onContactsItemTypeChange(item) {
    const id = item.key.split("_")[0];
    var stateCopy = Object.assign({}, this.state);
    stateCopy.profile.contacts.forEach(element => {
      if (element.id === id)
        element.type = item.value;
    });
    this.setState(stateCopy);
  }

  onContactsItemTextChange(event) {
    const id = event.target.name.split("_")[0];
    var stateCopy = Object.assign({}, this.state);
    stateCopy.profile.contacts.forEach(element => {
      if (element.id === id)
        element.value = event.target.value;
    });
    this.setState(stateCopy);
  }

  onShowGroupSelector() {
    var stateCopy = Object.assign({}, this.state);
    stateCopy.selector.visible = true;
    this.setState(stateCopy);
  }

  onCloseGroupSelector() {
    var stateCopy = Object.assign({}, this.state);
    stateCopy.selector.visible = false;
    this.setState(stateCopy);
  }

  onSearchGroups(template) {
    var stateCopy = Object.assign({}, this.state);
    stateCopy.selector.options = filterGroupSelectorOptions(stateCopy.selector.allOptions, template);
    this.setState(stateCopy);
  }

  onSelectGroups(selected) {
    var stateCopy = Object.assign({}, this.state);
    stateCopy.profile.groups = mapGroupSelectorOptionsToGroups(selected);
    stateCopy.selector.selected = selected;
    stateCopy.selector.visible = false;
    this.setState(stateCopy);
  }

  onRemoveGroup(id) {
    var stateCopy = Object.assign({}, this.state);
    stateCopy.profile.groups = stateCopy.profile.groups.filter(group => group.id !== id);
    stateCopy.selector.selected = stateCopy.selector.selected.filter(option => option.key !== id);
    this.setState(stateCopy)
  }

  render() {
    const { isLoading, errors, profile, dialog, selector } = this.state;
    const { t } = this.props;

    const pattern = getUserContactsPattern();
    const contacts = getUserContacts(profile.contacts);

    return (
      <>
        <MainContainer>
          <AvatarContainer>
            <Avatar
              size="max"
              role={getUserRole(profile)}
              source={profile.avatarMax}
              userName={profile.displayName}
              editing={true}
              editLabel={t("EditPhoto")}
            />
          </AvatarContainer>
          <MainFieldsContainer>
            <TextChangeField
              labelText={`${t("Email")}:`}
              inputName="email"
              inputValue={profile.email}
              buttonText={t("ChangeButton")}
              buttonIsDisabled={isLoading}
              buttonOnClick={this.onEmailChange}
              buttonTabIndex={1}
            />
            <TextChangeField
              labelText={`${t("Password")}:`}
              inputName="password"
              inputValue={profile.password}
              buttonText={t("ChangeButton")}
              buttonIsDisabled={isLoading}
              buttonOnClick={this.onPasswordChange}
              buttonTabIndex={2}
            />
            <TextChangeField
              labelText={`${t("Phone")}:`}
              inputName="phone"
              inputValue={profile.phone}
              buttonText={t("ChangeButton")}
              buttonIsDisabled={isLoading}
              buttonOnClick={this.onPhoneChange}
              buttonTabIndex={3}
            />
            <TextField
              isRequired={true}
              hasError={errors.firstName}
              labelText={`${t("FirstName")}:`}
              inputName="firstName"
              inputValue={profile.firstName}
              inputIsDisabled={isLoading}
              inputOnChange={this.onInputChange}
              inputAutoFocussed={true}
              inputTabIndex={4}
            />
            <TextField
              isRequired={true}
              hasError={errors.lastName}
              labelText={`${t("LastName")}:`}
              inputName="lastName"
              inputValue={profile.lastName}
              inputIsDisabled={isLoading}
              inputOnChange={this.onInputChange}
              inputTabIndex={5}
            />
            <DateField
              labelText={`${t("Birthdate")}:`}
              inputName="birthday"
              inputValue={profile.birthday ? new Date(profile.birthday) : undefined}
              inputIsDisabled={isLoading}
              inputOnChange={this.onBirthdayDateChange}
              inputTabIndex={6}
            />
            <RadioField
              labelText={`${t("Sex")}:`}
              radioName="sex"
              radioValue={profile.sex}
              radioOptions={[
                { value: 'male', label: t("SexMale")},
                { value: 'female', label: t("SexFemale")}
              ]}
              radioIsDisabled={isLoading}
              radioOnChange={this.onInputChange}
            />
            <RadioField
              labelText={`${t("UserType")}:`}
              radioName="isVisitor"
              radioValue={profile.isVisitor.toString()}
              radioOptions={[
                { value: "true", label: t("CustomTypeGuest", { typeGuest })},
                { value: "false", label: t("CustomTypeUser", { typeUser })}
              ]}
              radioIsDisabled={isLoading}
              radioOnChange={this.onUserTypeChange}
            />
            <DateField
              labelText={`${t("CustomEmployedSinceDate", { employedSinceDate })}:`}
              inputName="workFrom"
              inputValue={profile.workFrom ? new Date(profile.workFrom) : undefined}
              inputIsDisabled={isLoading}
              inputOnChange={this.onWorkFromDateChange}
              inputTabIndex={7}
            />
            <TextField
              labelText={`${t("Location")}:`}
              inputName="location"
              inputValue={profile.location}
              inputIsDisabled={isLoading}
              inputOnChange={this.onInputChange}
              inputTabIndex={8}
            />
            <TextField
              labelText={`${t("CustomPosition", { position })}:`}
              inputName="title"
              inputValue={profile.title}
              inputIsDisabled={isLoading}
              inputOnChange={this.onInputChange}
              inputTabIndex={9}
            />
            <DepartmentField
              labelText={`${t("CustomDepartment", { department })}:`}
              isDisabled={isLoading}
              showGroupSelectorButtonTitle={t("AddButton")}
              onShowGroupSelector={this.onShowGroupSelector}
              onCloseGroupSelector={this.onCloseGroupSelector}
              onRemoveGroup={this.onRemoveGroup}
              selectorIsVisible={selector.visible}
              selectorSearchPlaceholder={t("Search")}
              selectorOptions={selector.options}
              selectorSelectedOptions={selector.selected}
              selectorAddButtonText={t("CustomAddDepartments", { departments })}
              selectorSelectAllText={t("SelectAll")}
              selectorOnSearchGroups={this.onSearchGroups}
              selectorOnSelectGroups={this.onSelectGroups}
            />
          </MainFieldsContainer>
        </MainContainer>
        <InfoFieldContainer headerText={t("Comments")}>
          <Textarea name="notes" value={profile.notes} isDisabled={isLoading} onChange={this.onInputChange} tabIndex={10}/> 
        </InfoFieldContainer>
        <InfoFieldContainer headerText={t("ContactInformation")}>
          <ContactsField
            pattern={pattern.contact}
            contacts={contacts.contact}
            isDisabled={isLoading}
            addItemText={t("AddContact")}
            onItemAdd={this.onContactsItemAdd}
            onItemTypeChange={this.onContactsItemTypeChange}
            onItemTextChange={this.onContactsItemTextChange}
          /> 
        </InfoFieldContainer>
        <InfoFieldContainer headerText={t("SocialProfiles")}>
          <ContactsField
            pattern={pattern.social}
            contacts={contacts.social}
            isDisabled={isLoading}
            addItemText={t("AddContact")}
            onItemAdd={this.onContactsItemAdd}
            onItemTypeChange={this.onContactsItemTypeChange}
            onItemTextChange={this.onContactsItemTextChange}
          /> 
        </InfoFieldContainer>
        <div>
          <Button label={t("SaveButton")} onClick={this.handleSubmit} primary isDisabled={isLoading} size="big" tabIndex={11}/>
          <Button label={t("CancelButton")} onClick={this.onCancel} isDisabled={isLoading} size="big" style={{ marginLeft: "8px" }} tabIndex={12}/>
        </div>
        <ModalDialog
          visible={dialog.visible}
          headerContent={dialog.header}
          bodyContent={dialog.body}
          footerContent={dialog.buttons}
          onClose={this.onDialogClose}
        />
      </>
    );
  };
}

const mapStateToProps = (state) => {
  return {
    profile: state.profile.targetUser,
    settings: state.auth.settings,
    groups: state.people.groups
  }
};

export default connect(
  mapStateToProps,
  {
    updateProfile
  }
)(withRouter(withTranslation()(UpdateUserForm)));